using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services.Media;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

/// <summary>
/// Where a lesson lives. <see cref="VideoId"/> is null for a section's own completion. HasUpload: the video is an
/// uploaded file, whose length Streaming reports (UploadDurationSeconds). SectionHasVideos counts videos students can see.
/// </summary>
public sealed record LessonTarget(
    int CourseId, int SectionId, int? VideoId, string? ExternalUrl, bool HasUpload, int? UploadDurationSeconds, bool SectionHasVideos);

public interface ILessonProgressRepository : IRepository<LessonProgress>
{
    /// <summary>Read-only progress rows of one user in the given courses.</summary>
    Task<List<LessonProgress>> ListForUserAsync(int userId, IReadOnlyCollection<int> courseIds, CancellationToken ct);

    /// <summary>The tracked row for one user and lesson, or null if the user has not started it.</summary>
    Task<LessonProgress?> GetForLessonAsync(int userId, int sectionId, int? videoId, CancellationToken ct);

    /// <summary>The section and course of a video. Null when the video, its section or its course was deleted.</summary>
    Task<LessonTarget?> FindVideoAsync(int videoId, CancellationToken ct);

    /// <summary>The course of a section. Null when the section or its course was deleted.</summary>
    Task<LessonTarget?> FindSectionAsync(int sectionId, CancellationToken ct);
}

public sealed class LessonProgressRepository(AppDbContext db) : Repository<LessonProgress>(db), ILessonProgressRepository
{
    public Task<List<LessonProgress>> ListForUserAsync(int userId, IReadOnlyCollection<int> courseIds, CancellationToken ct) =>
        Query.Where(p => p.UserId == userId && courseIds.Contains(p.CourseId)).ToListAsync(ct);

    public Task<LessonProgress?> GetForLessonAsync(int userId, int sectionId, int? videoId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(p => p.UserId == userId && p.SectionId == sectionId && p.SectionVideoId == videoId, ct);

    public Task<LessonTarget?> FindVideoAsync(int videoId, CancellationToken ct) =>
        (from v in Db.SectionVideos
         join s in Db.Sections on v.SectionId equals s.Id
         join c in Db.Courses on s.CourseId equals c.Id
         where v.Id == videoId
         select new LessonTarget(c.Id, s.Id, v.Id, v.ExternalUrl, v.MediaAssetId != null, v.Asset != null ? v.Asset.DurationSeconds : null, true))
        .FirstOrDefaultAsync(ct);

    public Task<LessonTarget?> FindSectionAsync(int sectionId, CancellationToken ct)
    {
        // Same rule as CourseMapper.HasMedia: a link, or an uploaded file that is ready.
        var ready = MediaStatusCodes.Id(MediaStatusCodes.Ready);
        return (from s in Db.Sections
                join c in Db.Courses on s.CourseId equals c.Id
                where s.Id == sectionId
                select new LessonTarget(c.Id, s.Id, null, null, false, null, Db.SectionVideos.Any(v => v.SectionId == s.Id
                    && (v.MediaAssetId == null ? v.ExternalUrl != null : v.Asset!.MediaAssetStatusId == ready))))
            .FirstOrDefaultAsync(ct);
    }
}

public sealed record OverviewCounts(int Courses, int PublishedCourses, int Students, int ActiveEnrollments, decimal Collected);

/// <summary>Totals for the admin overview.</summary>
public interface IOverviewRepository
{
    Task<OverviewCounts> GetCountsAsync(CancellationToken ct);

    /// <summary>Newest payments first, with their status.</summary>
    Task<List<PaymentTransaction>> ListRecentPaymentsAsync(int take, CancellationToken ct);
}

public sealed class OverviewRepository(AppDbContext db) : IOverviewRepository
{
    public async Task<OverviewCounts> GetCountsAsync(CancellationToken ct) => new(
        await db.Courses.CountAsync(ct),
        await db.Courses.CountAsync(c => c.IsPublished, ct),
        await db.Users.CountAsync(u => u.Role.Code == Role.StudentCode, ct),
        await db.Enrollments.CountAsync(e => e.Status.Code == "active", ct),
        await db.PaymentTransactions.Where(t => t.Status.Code == "success").SumAsync(t => (decimal?)t.Amount, ct) ?? 0);

    public Task<List<PaymentTransaction>> ListRecentPaymentsAsync(int take, CancellationToken ct) =>
        db.PaymentTransactions.AsNoTracking().Include(t => t.Status)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Take(take).ToListAsync(ct);
}
