using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Media;
using CourseAcademy.Api.Services.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Services;

/// <summary>Catalogue reading and course management, on the in-memory database with real repositories.</summary>
public class CatalogTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    internal sealed class Kit
    {
        public IdentityTestKit Identity { get; } = new();
        public CatalogService Catalog { get; }
        public CourseAdminService Admin { get; }
        public InstructorService Instructors { get; }
        public EnrollmentService Enrollments { get; }
        public PaymentService Payments { get; }
        public PaymentGatewayResolver Gateways { get; }
        public ProgressService Progress { get; }
        public OverviewService Overview { get; }
        public MediaAssetRepository MediaAssets { get; }
        public int InstructorId { get; }

        public Kit()
        {
            var db = Identity.Db;
            var courses = new CourseRepository(db);
            var instructors = new InstructorRepository(db);
            var unitOfWork = new UnitOfWork(db);
            var enrollmentRepository = new EnrollmentRepository(db);
            Catalog = new CatalogService(courses, instructors, enrollmentRepository, Identity.CurrentUser);
            MediaAssets = new MediaAssetRepository(db);
            Admin = new CourseAdminService(courses, instructors, new LookupRepository(db),
                new Repository<CourseOutcome>(db), new Repository<CourseRequirement>(db), new Repository<LiveSession>(db),
                new Repository<Material>(db), new Repository<CourseModule>(db), new Repository<Submodule>(db), new Repository<Section>(db),
                new Repository<SectionVideo>(db), new Repository<SectionDocument>(db), new Repository<CourseGrant>(db),
                enrollmentRepository, MediaAssets, new MediaDeletionSignal(), unitOfWork, Identity.CurrentUser, Identity.Clock);
            Instructors = new InstructorService(instructors, new Repository<InstructorExpertise>(db), Identity.Users, unitOfWork, Identity.CurrentUser);
            Enrollments = new EnrollmentService(enrollmentRepository, courses, unitOfWork, Identity.CurrentUser, Identity.Clock);
            Gateways = new PaymentGatewayResolver([new DemoPaymentGateway()], Options.Create(new PaymentSettings { Gateway = "demo", PendingMinutes = 30 }));
            Payments = new PaymentService(new PaymentRepository(db), courses, enrollmentRepository, Enrollments, Identity.Users, new LookupRepository(db),
                Gateways, unitOfWork, Identity.CurrentUser, Options.Create(new PaymentSettings { Gateway = "demo", PendingMinutes = 30 }), Identity.Clock,
                NullLogger<PaymentService>.Instance);
            Progress = new ProgressService(new LessonProgressRepository(db), courses, enrollmentRepository, unitOfWork, Identity.CurrentUser, Identity.Clock);
            Overview = new OverviewService(new OverviewRepository(db), Identity.CurrentUser);

            var instructor = new Instructor { Name = "Tanvir Hasan", Title = "Editor", Bio = "Bio" };
            db.Instructors.Add(instructor);
            db.SaveChanges();
            InstructorId = instructor.Id;
            db.ChangeTracker.Clear();
        }

        public Kit As(string email)
        {
            Identity.As(email);
            return this;
        }

        /// <summary>An uploaded file of the course, as if it had been uploaded and processed by Streaming.</summary>
        public async Task<int> UploadedFileAsync(int courseId, string kind = "video", string status = "ready",
            string mimeType = "video/mp4", string fileName = "lesson.mp4")
        {
            var file = new MediaAsset
            {
                AssetId = "asset-" + Guid.NewGuid().ToString("N"), CourseId = courseId, UploadedByUserId = Identity.UserId(IdentityTestKit.Admin),
                MediaKindId = MediaKindCodes.Id(kind), MediaAssetStatusId = MediaStatusCodes.Id(status),
                FileName = fileName, MimeType = mimeType, SizeBytes = 1000,
            };
            Identity.Db.MediaAssets.Add(file);
            await Identity.Db.SaveChangesAsync();
            Identity.Db.ChangeTracker.Clear();
            return file.Id;
        }

        /// <summary>Turns a video into an older link video, like the seeded demo videos (new videos can only be uploaded).</summary>
        public async Task UseLinkAsync(int videoId, string url)
        {
            var video = await Identity.Db.SectionVideos.SingleAsync(v => v.Id == videoId);
            video.ExternalUrl = url;
            video.MediaAssetId = null;
            await Identity.Db.SaveChangesAsync();
            Identity.Db.ChangeTracker.Clear();
        }

        public string FileStatus(int mediaAssetId) =>
            MediaStatusCodes.Code(Identity.Db.MediaAssets.AsNoTracking().Single(a => a.Id == mediaAssetId).MediaAssetStatusId);

        public CourseUpsertRequest Course(string slug = "photo-basics", string format = "Pre-recorded", bool published = false,
            IReadOnlyList<LiveSessionInput>? schedule = null, IReadOnlyList<MaterialInput>? materials = null) => new(
            "Photography Basics", slug, "Learn your camera", "A starter course.", "video-editing", format, "Beginner", null,
            InstructorId, "Bangla", 5, 3000, 2500, "Photo", "bi-camera", null, false, published,
            ["Use manual mode", "  "], [], schedule ?? [], materials ?? []);
    }

    internal static SectionInput Section(string title, IReadOnlyList<VideoInput>? videos = null, IReadOnlyList<DocumentInput>? documents = null,
        IReadOnlyList<SectionInput>? topics = null) => new(null, title, videos ?? [], documents ?? [], topics ?? []);

    internal static CurriculumRequest TwoSections(VideoInput? video = null, DocumentInput? document = null) => new(
    [
        new ModuleInput(null, "Module", [new SubmoduleInput(null, "Submodule",
        [
            Section("First", video is null ? [] : [video], document is null ? [] : [document]),
            Section("Second"),
        ])]),
    ]);

    [Fact]
    public async Task Admin_creates_a_draft_with_trimmed_texts_and_month_stamp()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);

        var course = await kit.Admin.CreateAsync(kit.Course(), Ct);

        Assert.False(course.IsPublished);
        Assert.Equal(["Use manual mode"], course.Outcomes);
        Assert.Equal(17, course.DiscountPercent);
        Assert.Equal("2026-11", course.ContentUpdatedMonth);
    }

    [Fact]
    public async Task Slugs_are_unique_and_lookups_must_exist()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        await kit.Admin.CreateAsync(kit.Course(), Ct);

        var slug = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Admin.CreateAsync(kit.Course(), Ct));
        Assert.Equal("slug", slug.Errors.Single().Field);

        var format = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Admin.CreateAsync(kit.Course("other", format: "Podcast"), Ct));
        Assert.Equal("formatCode", format.Errors.Single().Field);
    }

    [Fact]
    public async Task A_course_needs_sections_and_live_courses_need_a_session_before_publishing()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        var draft = await kit.Admin.CreateAsync(kit.Course(format: "Live"), Ct);

        var noSections = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Admin.SetPublishedAsync(draft.Id, true, Ct));
        Assert.Equal(CourseAdminService.NeedsSectionsMessage, noSections.Message);

        await kit.Admin.SaveCurriculumAsync(draft.Id, TwoSections(), Ct);
        var noSession = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Admin.SetPublishedAsync(draft.Id, true, Ct));
        Assert.Equal(CourseAdminService.NeedsSessionsMessage, noSession.Message);

        var withSession = kit.Course(format: "Live", published: true, schedule: [new LiveSessionInput(null, "Class 1", "2026-12-01T19:00", 60, "https://meet.example.com/x")]);
        var published = await kit.Admin.UpdateAsync(draft.Id, withSession, Ct);

        Assert.True(published.IsPublished);
        Assert.Equal("2026-12-01T19:00", published.Schedule.Single().StartsAt);
        var stored = await kit.Identity.Db.LiveSessions.SingleAsync();
        Assert.Equal(new DateTime(2026, 12, 1, 13, 0, 0, DateTimeKind.Utc), stored.StartsAtUtc);
    }

    [Fact]
    public async Task Curriculum_save_creates_links_documents_to_new_videos_and_removes_what_is_left_out()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        var course = await kit.Admin.CreateAsync(kit.Course(), Ct);
        var file = await kit.UploadedFileAsync(course.Id);
        var first = await kit.Admin.SaveCurriculumAsync(course.Id, TwoSections(
            new VideoInput(null, "v1", "Intro", null, 5, true, file),
            new DocumentInput(null, "Notes", "PDF", "https://files.example.com/notes.pdf", null, "v1")), Ct);

        var section = first.Modules[0].Submodules[0].Sections[0];
        Assert.Equal(section.Videos[0].Id, section.Documents[0].VideoId);
        Assert.Equal((MediaSources.Upload, (string?)null, "ready"), (section.Videos[0].SourceType, section.Videos[0].Url, section.Videos[0].Media?.StatusCode));
        Assert.Equal(2, first.SectionCount);

        // Resend with ids, the video dropped: its document stays but is no longer attached.
        var second = new CurriculumRequest(
        [
            new ModuleInput(first.Modules[0].Id, "Renamed", [new SubmoduleInput(first.Modules[0].Submodules[0].Id, "Submodule",
            [
                new SectionInput(section.Id, "First", [], [new DocumentInput(section.Documents[0].Id, "Notes", "PDF", "https://files.example.com/notes.pdf", null, null)], []),
                Section("Third"),
            ])]),
        ]);
        var saved = await kit.Admin.SaveCurriculumAsync(course.Id, second, Ct);

        Assert.Equal("Renamed", saved.Modules[0].Title);
        Assert.Equal(["First", "Third"], saved.Modules[0].Submodules[0].Sections.Select(s => s.Title));
        Assert.Empty(saved.Modules[0].Submodules[0].Sections[0].Videos);
        Assert.Null(saved.Modules[0].Submodules[0].Sections[0].Documents[0].VideoId);
        Assert.True(await kit.Identity.Db.Sections.IgnoreQueryFilters().AnyAsync(s => s.Title == "Second" && s.IsDeleted));
        // The removed video's file is no longer used, so it is deleted from Streaming.
        Assert.Equal(MediaStatusCodes.Deleting, kit.FileStatus(file));
    }

    [Theory]
    [InlineData("one-section", "Each submodule needs 2 or 3 sections.")]
    [InlineData("no-file", "Upload a file for every new video.")]
    [InlineData("new-link", "New videos can't be added as links. Upload the video file.")]
    [InlineData("bad-extension", "A document link must end in .pdf, .docx or .pptx to match its type.")]
    [InlineData("too-deep", "Topics can be nested at most 3 levels deep.")]
    [InlineData("foreign-video", "A document is attached to a video that is not in its section.")]
    public void Curriculum_rules_report_the_first_broken_rule(string scenario, string expected)
    {
        var types = new Dictionary<string, DocumentType> { ["PDF"] = new() { Code = "PDF", FileExtension = ".pdf" } };
        var request = scenario switch
        {
            "one-section" => new CurriculumRequest([new ModuleInput(null, "M", [new SubmoduleInput(null, "S", [Section("A")])])]),
            "no-file" => TwoSections(new VideoInput(null, "v", "Intro", null, 5, false)),
            "new-link" => TwoSections(new VideoInput(null, "v", "Intro", "https://youtu.be/abcdefgh", 5, false)),
            "bad-extension" => TwoSections(document: new DocumentInput(null, "Notes", "PDF", "https://files.example.com/notes.docx", null, null)),
            "too-deep" => new CurriculumRequest([new ModuleInput(null, "M", [new SubmoduleInput(null, "S",
                [Section("A", topics: [Section("1", topics: [Section("2", topics: [Section("3", topics: [Section("4")])])])]), Section("B")])])]),
            _ => TwoSections(document: new DocumentInput(null, "Notes", "PDF", null, null, "missing")),
        };

        Assert.Equal(expected, CurriculumRules.FirstError(request, types));
    }

    [Fact]
    public async Task Visitors_see_published_courses_without_locked_links_or_file_less_items()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        var course = await kit.Admin.CreateAsync(kit.Course(materials: [new MaterialInput(null, "Guide", "PDF", null), new MaterialInput(null, "Site", "Link", "https://example.com/site")]), Ct);
        var saved = await kit.Admin.SaveCurriculumAsync(course.Id, new CurriculumRequest(
        [
            new ModuleInput(null, "M", [new SubmoduleInput(null, "S",
            [
                Section("A",
                    [
                        new VideoInput(null, "free", "Free", null, 5, true, await kit.UploadedFileAsync(course.Id)),
                        new VideoInput(null, "paid", "Paid", null, 5, false, await kit.UploadedFileAsync(course.Id)),
                        new VideoInput(null, "busy", "Processing", null, 5, false, await kit.UploadedFileAsync(course.Id, status: "processing")),
                    ],
                    [new DocumentInput(null, "Free notes", "PDF", "https://f.example.com/free.pdf", null, "free"),
                     new DocumentInput(null, "Section notes", "PDF", "https://f.example.com/all.pdf", null, null),
                     new DocumentInput(null, "No file yet", "PDF", null, null, null)]),
                Section("B"),
            ])]),
        ]), Ct);
        var videos = saved.Modules[0].Submodules[0].Sections[0].Videos;
        await kit.UseLinkAsync(videos[0].Id, "https://youtu.be/freefree");
        await kit.UseLinkAsync(videos[1].Id, "https://youtu.be/paidpaid");
        await kit.Admin.SetPublishedAsync(course.Id, true, Ct);
        await kit.Admin.CreateAsync(kit.Course("hidden-draft"), Ct);
        kit.Identity.CurrentUser.Set(null);

        var all = await kit.Catalog.GetAllAsync(Ct);
        var visible = Assert.Single(all);
        var section = visible.Modules[0].Submodules[0].Sections[0];

        Assert.False(visible.HasContentAccess);
        // An upload still processing is hidden (media rule B); visitors never get upload details.
        Assert.Equal(["Free", "Paid"], section.Videos.Select(v => v.Title));
        Assert.All(section.Videos, v => Assert.Null(v.Media));
        Assert.Equal("https://youtu.be/freefree", section.Videos.Single(v => v.Title == "Free").Url);
        Assert.Null(section.Videos.Single(v => v.Title == "Paid").Url);
        Assert.Equal(["Free notes", "Section notes"], section.Documents.Select(d => d.Title));
        Assert.False(section.Documents[0].IsLocked);
        Assert.True(section.Documents[1].IsLocked);
        Assert.Equal(["Site"], visible.Materials.Select(m => m.Title));
        Assert.Null(visible.Materials[0].Url);
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Catalog.GetBySlugAsync("hidden-draft", Ct));
    }

    [Fact]
    public async Task Search_filters_sorts_and_pages_published_courses()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        foreach (var (slug, price) in new[] { ("cheap", 1000m), ("middle", 3000m), ("costly", 6000m) })
        {
            var c = await kit.Admin.CreateAsync(kit.Course(slug) with { Price = price, OriginalPrice = 9000 }, Ct);
            await kit.Admin.SaveCurriculumAsync(c.Id, TwoSections(), Ct);
            await kit.Admin.SetPublishedAsync(c.Id, true, Ct);
        }

        var under = await kit.Catalog.SearchAsync(new CourseSearchQuery { Price = "under-2500" }, Ct);
        var sorted = await kit.Catalog.SearchAsync(new CourseSearchQuery { Sort = "price-desc", PageSize = 2 }, Ct);
        var levels = await kit.Catalog.SearchAsync(new CourseSearchQuery { Levels = "Advanced" }, Ct);

        Assert.Equal(["cheap"], under.Items.Select(c => c.Slug));
        Assert.Equal(3, sorted.Total);
        Assert.Equal(["costly", "middle"], sorted.Items.Select(c => c.Slug));
        Assert.Equal(2, sorted.Items[0].SectionCount);
        Assert.Empty(levels.Items);
    }

    [Fact]
    public async Task Instructors_with_courses_cannot_be_deleted()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        var course = await kit.Admin.CreateAsync(kit.Course(), Ct);

        var error = await Assert.ThrowsAsync<ConflictException>(() => kit.Instructors.DeleteAsync(kit.InstructorId, Ct));
        Assert.Equal("Tanvir Hasan teaches 1 course(s). Reassign them before deleting.", error.Message);

        await kit.Admin.DeleteAsync(course.Id, Ct);
        await kit.Instructors.DeleteAsync(kit.InstructorId, Ct);
        Assert.Empty(await kit.Catalog.GetInstructorsAsync(Ct));
    }

    [Fact]
    public async Task Course_grants_limit_editing_to_the_granted_course()
    {
        var kit = new Kit().As(IdentityTestKit.Admin);
        var mine = await kit.Admin.CreateAsync(kit.Course("mine"), Ct);
        var other = await kit.Admin.CreateAsync(kit.Course("other"), Ct);
        await kit.Identity.Access.SetCourseGrantAsync(kit.Identity.UserId(IdentityTestKit.Instructor), mine.Id, new SetCourseGrantRequest(["courses.view", "courses.edit"]), Ct);

        kit.As(IdentityTestKit.Instructor);
        await kit.Admin.SaveCurriculumAsync(mine.Id, TwoSections(), Ct);
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Admin.SaveCurriculumAsync(other.Id, TwoSections(), Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Admin.DeleteAsync(mine.Id, Ct));
        Assert.Equal(["mine"], (await kit.Catalog.GetAllAsync(Ct)).Select(c => c.Slug));
    }

    [Fact]
    public void Bangladesh_time_converts_both_ways()
    {
        Assert.True(BangladeshTime.TryParseToUtc("2026-11-20T20:00", out var utc));
        Assert.Equal(new DateTime(2026, 11, 20, 14, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal("2026-11-20T20:00", BangladeshTime.FromUtc(utc));
        Assert.False(BangladeshTime.TryParseToUtc("20/11/2026", out _));
    }
}
