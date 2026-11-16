using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;

namespace CourseAcademy.Tests.Services;

/// <summary>Lesson progress (BR-LRN-15 to 19) and the admin overview.</summary>
public class ProgressTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string YouTube = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    private const string Mp4 = "https://cdn.example.com/lesson.mp4";

    /// <summary>Lesson ids of the test course, as the enrolled student sees them.</summary>
    private sealed record Lessons(int YouTubeVideo, int FileVideo, int TopicVideo, int IntroSection, int ReadingSection);

    /// <summary>
    /// A free published course with 4 lessons: "Intro" (a YouTube video and an .mp4 file), its topic "Deep dive"
    /// (one video) and "Reading" (no videos). The videos are older link videos, like the seeded demo course.
    /// Optionally enrolls the demo student, who is signed in afterwards.
    /// </summary>
    private static async Task<(CatalogTests.Kit Kit, CourseDetailDto Course, Lessons Lessons)> SetUp(bool enroll = true)
    {
        var kit = new CatalogTests.Kit().As(IdentityTestKit.Admin);
        var created = await kit.Admin.CreateAsync(kit.Course("free-course") with { OriginalPrice = 0, Price = 0 }, Ct);
        var saved = await kit.Admin.SaveCurriculumAsync(created.Id, new CurriculumRequest(
        [
            new ModuleInput(null, "Module", [new SubmoduleInput(null, "Submodule",
            [
                CatalogTests.Section("Intro",
                    [
                        new VideoInput(null, null, "Welcome", null, 5, false, await kit.UploadedFileAsync(created.Id)),
                        new VideoInput(null, null, "Camera tour", null, 10, false, await kit.UploadedFileAsync(created.Id)),
                    ],
                    topics: [CatalogTests.Section("Deep dive", [new VideoInput(null, null, "Settings", null, 7, false, await kit.UploadedFileAsync(created.Id))])]),
                CatalogTests.Section("Reading"),
            ])]),
        ]), Ct);
        var sections = saved.Modules[0].Submodules[0].Sections;
        var intro = sections[0];
        var lessons = new Lessons(intro.Videos[0].Id, intro.Videos[1].Id, intro.Topics[0].Videos[0].Id, intro.Id, sections[1].Id);
        await kit.UseLinkAsync(lessons.YouTubeVideo, YouTube);
        await kit.UseLinkAsync(lessons.FileVideo, Mp4);
        await kit.UseLinkAsync(lessons.TopicVideo, YouTube);
        var course = await kit.Admin.SetPublishedAsync(created.Id, true, Ct);

        kit.As(IdentityTestKit.Student);
        if (enroll) await kit.Enrollments.EnrollFreeAsync("free-course", Ct);
        return (kit, course, lessons);
    }

    [Fact]
    public async Task Only_enrolled_learners_have_progress_and_each_video_or_video_less_section_is_one_lesson()
    {
        var (kit, course, lessons) = await SetUp(enroll: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Progress.GetCourseAsync(course.Slug, Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Progress.SetVideoCompletedAsync(lessons.YouTubeVideo, true, Ct));
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Progress.GetCourseAsync("missing", Ct));

        await kit.Enrollments.EnrollFreeAsync(course.Slug, Ct);
        var progress = await kit.Progress.GetCourseAsync(course.Slug, Ct);

        Assert.Equal((0, 4, 0), (progress.CompletedLessons, progress.TotalLessons, progress.Percent));
        Assert.Empty(progress.CompletedSectionIds);
    }

    [Fact]
    public async Task Lessons_are_marked_and_unmarked_and_a_section_is_done_when_all_its_lessons_are()
    {
        var (kit, _, lessons) = await SetUp();

        await kit.Progress.SetVideoCompletedAsync(lessons.YouTubeVideo, true, Ct);
        await kit.Progress.SetSectionCompletedAsync(lessons.ReadingSection, true, Ct);
        var half = await kit.Progress.SetVideoCompletedAsync(lessons.TopicVideo, true, Ct);

        Assert.Equal((3, 75), (half.CompletedLessons, half.Percent));
        // Intro still has its file video open; its topic is a section of its own.
        Assert.DoesNotContain(lessons.IntroSection, half.CompletedSectionIds);
        Assert.Contains(lessons.ReadingSection, half.CompletedSectionIds);

        var all = await kit.Progress.SetVideoCompletedAsync(lessons.FileVideo, true, Ct);
        Assert.Equal(100, all.Percent);
        Assert.Contains(lessons.IntroSection, all.CompletedSectionIds);

        var undone = await kit.Progress.SetSectionCompletedAsync(lessons.ReadingSection, false, Ct);
        Assert.Equal((3, 75), (undone.CompletedLessons, undone.Percent));
        Assert.DoesNotContain(lessons.ReadingSection, undone.CompletedSectionIds);
    }

    [Fact]
    public async Task A_section_with_videos_is_completed_through_its_videos()
    {
        var (kit, _, lessons) = await SetUp();

        await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Progress.SetSectionCompletedAsync(lessons.IntroSection, true, Ct));
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Progress.SetVideoCompletedAsync(999_999, true, Ct));
    }

    [Fact]
    public async Task A_video_file_saves_its_position_and_completes_itself_at_ninety_percent_once()
    {
        var (kit, _, lessons) = await SetUp();

        var watching = await kit.Progress.SavePositionAsync(lessons.FileVideo, new VideoPositionRequest(80, 100), Ct);
        var video = Assert.Single(watching.Videos);
        Assert.Equal((80, 100, false), (video.PositionSeconds, video.DurationSeconds, video.IsCompleted));

        var done = await kit.Progress.SavePositionAsync(lessons.FileVideo, new VideoPositionRequest(90, 100), Ct);
        Assert.True(Assert.Single(done.Videos).IsCompleted);

        // Marked as not done by the learner: watching on does not complete it again.
        await kit.Progress.SetVideoCompletedAsync(lessons.FileVideo, false, Ct);
        var later = await kit.Progress.SavePositionAsync(lessons.FileVideo, new VideoPositionRequest(99, 100), Ct);
        Assert.Equal((99, false), (later.Videos[0].PositionSeconds, later.Videos[0].IsCompleted));

        // A position past the end is stored as the end.
        var end = await kit.Progress.SavePositionAsync(lessons.FileVideo, new VideoPositionRequest(130, 100), Ct);
        Assert.Equal(100, end.Videos[0].PositionSeconds);
    }

    [Fact]
    public async Task An_embedded_video_has_no_position()
    {
        var (kit, _, lessons) = await SetUp();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            kit.Progress.SavePositionAsync(lessons.YouTubeVideo, new VideoPositionRequest(10, 100), Ct));
    }

    [Fact]
    public async Task Revoked_learners_cannot_record_progress_but_still_see_it_on_the_dashboard()
    {
        var (kit, course, lessons) = await SetUp();
        await kit.Progress.SetVideoCompletedAsync(lessons.YouTubeVideo, true, Ct);

        kit.As(IdentityTestKit.Admin);
        var enrollment = (await kit.Enrollments.GetAllAsync(new EnrollmentListQuery(), Ct)).Items.Single();
        await kit.Enrollments.SetStatusAsync(enrollment.Id, EnrollmentStatusCodes.Revoked, Ct);
        kit.As(IdentityTestKit.Student);

        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Progress.SetVideoCompletedAsync(lessons.FileVideo, true, Ct));
        var summary = Assert.Single(await kit.Progress.GetMineAsync(Ct));
        Assert.Equal((course.Id, 1, 4, 25), (summary.CourseId, summary.CompletedLessons, summary.TotalLessons, summary.Percent));
    }

    [Fact]
    public async Task Course_managers_can_record_progress_without_an_enrollment()
    {
        var (kit, course, lessons) = await SetUp(enroll: false);
        kit.As(IdentityTestKit.Admin);

        var progress = await kit.Progress.SetVideoCompletedAsync(lessons.YouTubeVideo, true, Ct);

        Assert.Equal(1, progress.CompletedLessons);
        Assert.Equal(course.Id, progress.CourseId);
    }

    [Fact]
    public async Task Removed_lessons_no_longer_count()
    {
        var (kit, course, lessons) = await SetUp();
        await kit.Progress.SetVideoCompletedAsync(lessons.TopicVideo, true, Ct);

        kit.As(IdentityTestKit.Admin);
        var current = await kit.Catalog.GetBySlugAsync(course.Slug, Ct);
        var intro = current.Modules[0].Submodules[0].Sections[0];
        // Keep everything except the "Deep dive" topic.
        await kit.Admin.SaveCurriculumAsync(course.Id, new CurriculumRequest(
        [
            new ModuleInput(current.Modules[0].Id, "Module", [new SubmoduleInput(current.Modules[0].Submodules[0].Id, "Submodule",
            [
                new SectionInput(intro.Id, "Intro",
                    intro.Videos.Select(v => new VideoInput(v.Id, null, v.Title, v.Url, v.Minutes, v.IsPreview)).ToList(), [], []),
                new SectionInput(lessons.ReadingSection, "Reading", [], [], []),
            ])]),
        ]), Ct);
        kit.As(IdentityTestKit.Student);

        var progress = await kit.Progress.GetCourseAsync(course.Slug, Ct);
        Assert.Equal((0, 3), (progress.CompletedLessons, progress.TotalLessons));
        Assert.Empty(progress.Videos);
    }

    [Fact]
    public async Task The_overview_counts_courses_students_enrollments_and_collected_money()
    {
        var (kit, _, _) = await SetUp();

        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Overview.GetAsync(Ct));

        kit.As(IdentityTestKit.Admin);
        var overview = await kit.Overview.GetAsync(Ct);
        Assert.Equal((1, 1, 1, 0m), (overview.CourseCount, overview.PublishedCourseCount, overview.ActiveEnrollmentCount, overview.CollectedAmount));
        Assert.True(overview.StudentCount >= 2);
        Assert.Empty(overview.RecentTransactions);
    }
}
