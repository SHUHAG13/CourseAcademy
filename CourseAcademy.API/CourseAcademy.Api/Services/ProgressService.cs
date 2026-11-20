using System.Text.RegularExpressions;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Services;

/// <summary>The signed-in user's learning progress, per lesson (BR-LRN-15 to 19).</summary>
public interface IProgressService
{
    Task<CourseProgressDto> GetCourseAsync(string courseSlug, CancellationToken ct);

    /// <summary>Progress in every course the user is enrolled in, for the dashboard.</summary>
    Task<IReadOnlyList<CourseProgressSummaryDto>> GetMineAsync(CancellationToken ct);

    Task<CourseProgressDto> SetVideoCompletedAsync(int videoId, bool completed, CancellationToken ct);

    /// <summary>Only for sections without videos; other sections are completed through their videos.</summary>
    Task<CourseProgressDto> SetSectionCompletedAsync(int sectionId, bool completed, CancellationToken ct);

    /// <summary>Saves where the user is in a video, and completes it once 90% has been watched (BR-LRN-17).</summary>
    Task<CourseProgressDto> SavePositionAsync(int videoId, VideoPositionRequest request, CancellationToken ct);
}

public static partial class LessonRules
{
    /// <summary>Share of a video that must be watched before it completes by itself.</summary>
    public const double AutoCompleteShare = 0.9;

    /// <summary>Same rule as the frontend's isVideoFile: a direct https .mp4/.webm link.</summary>
    [GeneratedRegex(@"^https://\S+\.(mp4|webm)(\?\S*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex VideoFile();

    /// <summary>
    /// Whether the player can report the position: uploaded videos and direct video files. YouTube and Vimeo
    /// embeds cannot, so they are completed with the button only.
    /// </summary>
    public static bool CanTrackPosition(string? externalUrl, bool hasUpload) =>
        hasUpload || (externalUrl is not null && VideoFile().IsMatch(externalUrl));

    public static int Percent(int completed, int total) =>
        total == 0 ? 0 : (int)Math.Round(completed * 100.0 / total, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Builds a course's progress from its current curriculum. Each video students can see is one lesson, and so is
    /// each section without such videos (BR-LRN-15). Videos with no file yet, or an upload that is not ready, do not
    /// count. Rows for removed videos or sections are ignored (BR-LRN-05). Needs the videos' assets loaded.
    /// </summary>
    public static CourseProgressDto Build(int courseId, IReadOnlyCollection<Section> sections, IReadOnlyCollection<LessonProgress> rows)
    {
        var doneVideos = rows.Where(r => r.IsCompleted && r.SectionVideoId is not null).Select(r => r.SectionVideoId!.Value).ToHashSet();
        var doneSections = rows.Where(r => r.IsCompleted && r.SectionVideoId is null).Select(r => r.SectionId).ToHashSet();
        var visible = sections.ToDictionary(s => s.Id, s => s.Videos.Where(CourseMapper.HasMedia).ToList());
        var videoIds = visible.Values.SelectMany(v => v).Select(v => v.Id).ToHashSet();

        int total = 0, completed = 0;
        var completedSectionIds = new List<int>();
        foreach (var section in sections.OrderBy(s => s.Id))
        {
            var shown = visible[section.Id];
            var lessons = shown.Count == 0 ? 1 : shown.Count;
            var done = shown.Count == 0
                ? (doneSections.Contains(section.Id) ? 1 : 0)
                : shown.Count(v => doneVideos.Contains(v.Id));
            total += lessons;
            completed += done;
            if (done == lessons) completedSectionIds.Add(section.Id);
        }

        var videos = rows.Where(r => r.SectionVideoId is { } id && videoIds.Contains(id))
            .OrderBy(r => r.SectionVideoId)
            .Select(r => new VideoProgressDto(r.SectionVideoId!.Value, r.PositionSeconds, r.DurationSeconds, r.IsCompleted))
            .ToList();
        return new CourseProgressDto(courseId, completed, total, Percent(completed, total), completedSectionIds, videos);
    }
}

public sealed class ProgressService(
    ILessonProgressRepository progress,
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    ICertificationService certification,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IProgressService
{
    public async Task<CourseProgressDto> GetCourseAsync(string courseSlug, CancellationToken ct)
    {
        var course = (await courses.ListAsync(c => c.Slug == courseSlug, ct)).FirstOrDefault() ?? throw new NotFoundException("Course not found.");
        var userId = await EnsureLearnerAsync(course.Id, course.IsPublished, ct);
        return await BuildAsync(userId, course.Id, ct);
    }

    public async Task<IReadOnlyList<CourseProgressSummaryDto>> GetMineAsync(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var courseIds = (await enrollments.ListAsync(e => e.UserId == userId, ct)).Select(e => e.CourseId).Distinct().ToList();
        if (courseIds.Count == 0) return [];

        var sections = (await courses.ListSectionsAsync(courseIds, tracking: false, ct)).ToLookup(s => s.CourseId);
        var rows = (await progress.ListForUserAsync(userId, courseIds, ct)).ToLookup(r => r.CourseId);
        return courseIds.Order().Select(id =>
        {
            var p = LessonRules.Build(id, sections[id].ToList(), rows[id].ToList());
            return new CourseProgressSummaryDto(id, p.CompletedLessons, p.TotalLessons, p.Percent);
        }).ToList();
    }

    public async Task<CourseProgressDto> SetVideoCompletedAsync(int videoId, bool completed, CancellationToken ct)
    {
        var target = await progress.FindVideoAsync(videoId, ct) ?? throw new NotFoundException("Video not found.");
        var userId = await EnsureLearnerAsync(target.CourseId, published: true, ct);
        var row = await GetOrAddAsync(userId, target, ct);
        SetCompleted(row, completed);
        await SaveAsync(ct);
        if (completed) await certification.IssueForProgressAsync(userId, target.CourseId, ct);
        return await BuildAsync(userId, target.CourseId, ct);
    }

    public async Task<CourseProgressDto> SetSectionCompletedAsync(int sectionId, bool completed, CancellationToken ct)
    {
        var target = await progress.FindSectionAsync(sectionId, ct) ?? throw new NotFoundException("Section not found.");
        var userId = await EnsureLearnerAsync(target.CourseId, published: true, ct);
        if (target.SectionHasVideos) throw new BusinessRuleException("Mark the videos of this section as complete instead.");
        var row = await GetOrAddAsync(userId, target, ct);
        SetCompleted(row, completed);
        await SaveAsync(ct);
        if (completed) await certification.IssueForProgressAsync(userId, target.CourseId, ct);
        return await BuildAsync(userId, target.CourseId, ct);
    }

    public async Task<CourseProgressDto> SavePositionAsync(int videoId, VideoPositionRequest request, CancellationToken ct)
    {
        var target = await progress.FindVideoAsync(videoId, ct) ?? throw new NotFoundException("Video not found.");
        var userId = await EnsureLearnerAsync(target.CourseId, published: true, ct);
        if (!LessonRules.CanTrackPosition(target.ExternalUrl, target.HasUpload))
        {
            throw new BusinessRuleException("This video's position can't be saved. Use Mark as complete.");
        }

        var row = await GetOrAddAsync(userId, target, ct);
        // For uploads, the length Streaming measured counts, not what the browser says.
        var duration = target.UploadDurationSeconds is > 0 ? target.UploadDurationSeconds.Value : request.DurationSeconds;
        var position = Math.Min(request.PositionSeconds, duration);
        var threshold = (int)Math.Ceiling(duration * LessonRules.AutoCompleteShare);
        var reachedBefore = row.MaxWatchedSeconds >= threshold;

        row.PositionSeconds = position;
        row.DurationSeconds = duration;
        row.MaxWatchedSeconds = Math.Max(row.MaxWatchedSeconds, position);
        // Completes once, when 90% is first reached, so a lesson the user marked as not done stays that way.
        var completesNow = !row.IsCompleted && !reachedBefore && row.MaxWatchedSeconds >= threshold;
        if (completesNow) SetCompleted(row, true);

        await SaveAsync(ct);
        // Reaching the requirement issues a completion certificate (BR-CERT-04).
        if (completesNow) await certification.IssueForProgressAsync(userId, target.CourseId, ct);
        return await BuildAsync(userId, target.CourseId, ct);
    }

    /// <summary>
    /// Learning needs <c>learn.view</c> and course access: an active enrollment or managing the course (BR-LRN-02).
    /// Returns the user id.
    /// </summary>
    private async Task<int> EnsureLearnerAsync(int courseId, bool published, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        if (!access.Can(PermissionCodes.LearnView, null)) throw new ForbiddenException();
        if (access.Can(PermissionCodes.CoursesView, courseId)) return access.UserId;
        var enrollment = await enrollments.GetForUserCourseAsync(access.UserId, courseId, ct);
        if (enrollment?.Status.Code == EnrollmentStatusCodes.Active) return access.UserId;
        // An unpublished course looks like a missing one to people who may not open it (BR-CAT-02).
        throw published ? new ForbiddenException("Enroll in this course to track your progress.") : new NotFoundException("Course not found.");
    }

    private async Task<LessonProgress> GetOrAddAsync(int userId, LessonTarget target, CancellationToken ct)
    {
        var row = await progress.GetForLessonAsync(userId, target.SectionId, target.VideoId, ct);
        if (row is not null) return row;
        row = new LessonProgress { UserId = userId, CourseId = target.CourseId, SectionId = target.SectionId, SectionVideoId = target.VideoId };
        await progress.AddAsync(row, ct);
        return row;
    }

    private void SetCompleted(LessonProgress row, bool completed)
    {
        if (row.IsCompleted == completed) return;
        row.IsCompleted = completed;
        row.CompletedAt = completed ? clock.GetUtcNow().UtcDateTime : null;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            // Another request (a second tab, a double click) created this lesson's row at the same moment.
            // Its change stands; the response shows the stored state.
        }
    }

    private async Task<CourseProgressDto> BuildAsync(int userId, int courseId, CancellationToken ct)
    {
        var sections = await courses.ListSectionsAsync([courseId], tracking: false, ct);
        var rows = await progress.ListForUserAsync(userId, [courseId], ct);
        return LessonRules.Build(courseId, sections, rows);
    }
}
