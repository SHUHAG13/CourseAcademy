using System.Globalization;
using System.Text.Json;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CourseAcademy.Tests.Services;

/// <summary>Uploads, playback/download access, webhooks and file deletion (streaming spec), with a substitute Streaming service.</summary>
public class MediaTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string Secret = "test-webhook-secret-of-at-least-32-chars";

    private sealed class MediaKit
    {
        public CatalogTests.Kit Kit { get; } = new();
        public IStreamingClient Streaming { get; } = Substitute.For<IStreamingClient>();
        public MediaService Media { get; }
        public StreamingWebhookService Webhooks { get; }
        public MediaDeletionSender Deletions { get; }

        public MediaKit()
        {
            var db = Kit.Identity.Db;
            var settings = Options.Create(new StreamingSettings { Mode = StreamingSettings.HttpMode, WebhookSecret = Secret, LinkMinutes = 10 });
            Media = new MediaService(Kit.MediaAssets, new CourseRepository(db), new EnrollmentRepository(db), Streaming, new MediaDeletionSignal(),
                new UnitOfWork(db), Kit.Identity.CurrentUser, settings, Kit.Identity.Clock, NullLogger<MediaService>.Instance);
            Webhooks = new StreamingWebhookService(Kit.MediaAssets, new Repository<StreamingWebhookEvent>(db), new UnitOfWork(db), settings,
                Kit.Identity.Clock, NullLogger<StreamingWebhookService>.Instance);
            Deletions = new MediaDeletionSender(Kit.MediaAssets, Streaming, new UnitOfWork(db), Kit.Identity.Clock, NullLogger<MediaDeletionSender>.Instance);

            Streaming.CreateUploadAsync(Arg.Any<StreamingUploadRequest>(), Arg.Any<CancellationToken>())
                .Returns(call => new StreamingUpload("asset-" + call.Arg<StreamingUploadRequest>().ExternalRef, "https://storage.example.com/up",
                    "PUT", new Dictionary<string, string>(), Kit.Identity.Clock.GetUtcNow().UtcDateTime.AddMinutes(15)));
            Streaming.CompleteUploadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("uploaded");
            Streaming.CreatePlaybackAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(call => new StreamingLink($"https://cdn.example.com/{call.ArgAt<string>(0)}.m3u8", DateTime.UtcNow.AddMinutes(10), "hls"));
            Streaming.CreateDownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => new StreamingLink($"https://cdn.example.com/{call.ArgAt<string>(0)}", DateTime.UtcNow.AddMinutes(10), "file"));
        }

        public string AssetIdOf(int mediaAssetId) => Kit.Identity.Db.MediaAssets.AsNoTracking().Single(a => a.Id == mediaAssetId).AssetId!;

        /// <summary>A free published course with one section of the given videos, and the student enrolled when asked.</summary>
        public async Task<(CourseDetailDto Course, IReadOnlyList<VideoDto> Videos)> CourseWithVideosAsync(bool enroll, params (string Title, bool Preview, string Status)[] videos)
        {
            Kit.As(IdentityTestKit.Admin);
            var course = await Kit.Admin.CreateAsync(Kit.Course("media-course") with { OriginalPrice = 0, Price = 0 }, Ct);
            var inputs = new List<VideoInput>();
            foreach (var (title, preview, status) in videos)
            {
                inputs.Add(new VideoInput(null, null, title, null, 5, preview, await Kit.UploadedFileAsync(course.Id, status: status)));
            }
            var saved = await Kit.Admin.SaveCurriculumAsync(course.Id, new CurriculumRequest(
            [
                new ModuleInput(null, "M", [new SubmoduleInput(null, "S", [CatalogTests.Section("A", inputs), CatalogTests.Section("B")])]),
            ]), Ct);
            await Kit.Admin.SetPublishedAsync(course.Id, true, Ct);
            Kit.As(IdentityTestKit.Student);
            if (enroll) await Kit.Enrollments.EnrollFreeAsync(course.Slug, Ct);
            return (saved, saved.Modules[0].Submodules[0].Sections[0].Videos);
        }

        /// <summary>A webhook as the Streaming service would send it, signed with the shared secret.</summary>
        public Task<WebhookResult> SendAsync(string assetId, string type, long sequence, string? eventId = null, object? data = null,
            string secret = Secret, TimeSpan? age = null)
        {
            eventId ??= Guid.NewGuid().ToString();
            var body = JsonSerializer.Serialize(new { eventId, type, occurredAt = DateTime.UtcNow, assetId, sequence, data },
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var timestamp = (Kit.Identity.Clock.GetUtcNow() - (age ?? TimeSpan.Zero)).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [StreamingWebhookService.EventIdHeader] = eventId,
                [StreamingWebhookService.TimestampHeader] = timestamp,
                [StreamingWebhookService.SignatureHeader] = WebhookSignature.Compute(secret, timestamp, body),
            };
            return Webhooks.HandleAsync(body, headers, Ct);
        }
    }

    [Fact]
    public async Task Uploads_are_checked_before_the_streaming_service_is_called()
    {
        var kit = new MediaKit();
        kit.Kit.As(IdentityTestKit.Admin);
        var course = await kit.Kit.Admin.CreateAsync(kit.Kit.Course(), Ct);

        kit.Kit.As(IdentityTestKit.Student);
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Media.CreateUploadAsync(new CreateUploadRequest(course.Id, "video", "a.mp4", "video/mp4", 100), Ct));
        kit.Kit.As(IdentityTestKit.Admin);
        var wrongType = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            kit.Media.CreateUploadAsync(new CreateUploadRequest(course.Id, "video", "a.avi", "video/x-msvideo", 100), Ct));
        var tooBig = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            kit.Media.CreateUploadAsync(new CreateUploadRequest(course.Id, "document", "a.pdf", "application/pdf", MediaRules.MaxDocumentBytes + 1), Ct));
        await kit.Streaming.DidNotReceiveWithAnyArgs().CreateUploadAsync(default!, default);
        Assert.Equal("Upload an MP4, WebM or MOV video.", wrongType.Message);
        Assert.Equal("A document can be up to 50 MB.", tooBig.Message);

        var upload = await kit.Media.CreateUploadAsync(new CreateUploadRequest(course.Id, "video", @"C:\clips\intro.mp4", "VIDEO/MP4", 100), Ct);
        var pending = await kit.Media.GetAsync(upload.MediaAssetId, Ct);
        var done = await kit.Media.CompleteUploadAsync(upload.MediaAssetId, Ct);

        Assert.Equal("https://storage.example.com/up", upload.UploadUrl);
        Assert.Equal(("pending_upload", "intro.mp4", "video/mp4"), (pending.StatusCode, pending.FileName, pending.MimeType));
        Assert.Equal("uploaded", done.StatusCode);
        Assert.Equal($"asset-{upload.MediaAssetId}", kit.AssetIdOf(upload.MediaAssetId));
    }

    [Fact]
    public async Task Webhooks_are_verified_stored_once_and_applied_in_order()
    {
        var kit = new MediaKit();
        kit.Kit.As(IdentityTestKit.Admin);
        var course = await kit.Kit.Admin.CreateAsync(kit.Kit.Course(), Ct);
        var file = await kit.Kit.UploadedFileAsync(course.Id, status: "uploaded");
        var assetId = kit.AssetIdOf(file);

        Assert.Equal(WebhookResult.Unauthorized, await kit.SendAsync(assetId, "asset.ready", 9, secret: "a-different-secret-of-32-characters!!"));
        Assert.Equal(WebhookResult.Unauthorized, await kit.SendAsync(assetId, "asset.ready", 9, age: TimeSpan.FromMinutes(6)));
        Assert.Equal("uploaded", kit.Kit.FileStatus(file));

        Assert.Equal(WebhookResult.Accepted, await kit.SendAsync(assetId, "asset.processing", 2, eventId: "event-2"));
        // A repeat is acknowledged but stored and applied only once.
        Assert.Equal(WebhookResult.Accepted, await kit.SendAsync(assetId, "asset.processing", 2, eventId: "event-2"));
        // An older event arriving late does not move the status back.
        Assert.Equal(WebhookResult.Accepted, await kit.SendAsync(assetId, "asset.queued", 1));
        Assert.Equal("processing", kit.Kit.FileStatus(file));

        await kit.SendAsync(assetId, "asset.ready", 3, data: new { durationSeconds = 612 });
        Assert.Equal(WebhookResult.Accepted, await kit.SendAsync("unknown-asset", "asset.ready", 1));

        var stored = await kit.Kit.Identity.Db.StreamingWebhookEvents.AsNoTracking().OrderBy(e => e.Id).Select(e => e.Outcome).ToListAsync();
        Assert.Equal(["applied", "stale", "applied", "unknown_asset"], stored);
        var asset = await kit.Kit.Identity.Db.MediaAssets.AsNoTracking().SingleAsync(a => a.Id == file);
        Assert.Equal(("ready", 612, 3L), (kit.Kit.FileStatus(file), asset.DurationSeconds, asset.LastSequence));
    }

    [Fact]
    public async Task Playback_needs_course_access_and_a_ready_file_but_previews_are_open()
    {
        var kit = new MediaKit();
        var (_, videos) = await kit.CourseWithVideosAsync(enroll: false, ("Preview", true, "ready"), ("Lesson", false, "ready"), ("Busy", false, "processing"));
        var preview = videos.Single(v => v.Title == "Preview");
        var lesson = videos.Single(v => v.Title == "Lesson");
        var busy = videos.Single(v => v.Title == "Busy");

        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Media.GetVideoPlaybackAsync(lesson.Id, Ct));
        kit.Kit.Identity.CurrentUser.Set(null);
        await Assert.ThrowsAsync<UnauthorizedException>(() => kit.Media.GetVideoPlaybackAsync(lesson.Id, Ct));
        var guest = await kit.Media.GetVideoPlaybackAsync(preview.Id, Ct);
        Assert.Equal("hls", guest.Format);

        kit.Kit.As(IdentityTestKit.Student);
        await kit.Kit.Enrollments.EnrollFreeAsync("media-course", Ct);
        var played = await kit.Media.GetVideoPlaybackAsync(lesson.Id, Ct);
        await Assert.ThrowsAsync<ConflictException>(() => kit.Media.GetVideoPlaybackAsync(busy.Id, Ct));

        Assert.EndsWith(".m3u8", played.Url);
        var studentId = kit.Kit.Identity.UserId(IdentityTestKit.Student);
        await kit.Streaming.Received(1).CreatePlaybackAsync(Arg.Any<string>(), $"user:{studentId}", 600, Arg.Any<CancellationToken>());
        await kit.Streaming.Received(1).CreatePlaybackAsync(Arg.Any<string>(), "guest", 600, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Only_suitable_files_of_the_same_course_can_be_attached()
    {
        var kit = new MediaKit();
        kit.Kit.As(IdentityTestKit.Admin);
        var course = await kit.Kit.Admin.CreateAsync(kit.Kit.Course("one"), Ct);
        var other = await kit.Kit.Admin.CreateAsync(kit.Kit.Course("two"), Ct);

        Task Save(VideoInput? video = null, DocumentInput? document = null) =>
            kit.Kit.Admin.SaveCurriculumAsync(course.Id, CatalogTests.TwoSections(video, document), Ct);

        var foreign = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Save(new VideoInput(null, "v", "Intro", null, 5, false, kit.Kit.UploadedFileAsync(other.Id).Result)));
        var wrongKind = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Save(new VideoInput(null, "v", "Intro", null, 5, false, kit.Kit.UploadedFileAsync(course.Id, "document", mimeType: "application/pdf", fileName: "a.pdf").Result)));
        var failed = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Save(new VideoInput(null, "v", "Intro", null, 5, false, kit.Kit.UploadedFileAsync(course.Id, status: "failed").Result)));
        var docx = await kit.Kit.UploadedFileAsync(course.Id, "document",
            mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName: "notes.docx");
        var mismatch = await Assert.ThrowsAsync<BusinessRuleException>(() => Save(document: new DocumentInput(null, "Notes", "PDF", null, null, null, docx)));

        Assert.Equal("An uploaded file was not found. Upload it again.", foreign.Message);
        Assert.Equal("Upload a video file for a video.", wrongKind.Message);
        Assert.Equal("\"lesson.mp4\" could not be processed. Upload it again.", failed.Message);
        Assert.Equal("\"notes.docx\" must be a .pdf file to match the document type.", mismatch.Message);
    }

    [Fact]
    public async Task Older_link_videos_keep_their_link_but_cannot_change_it()
    {
        var kit = new MediaKit();
        var (course, videos) = await kit.CourseWithVideosAsync(enroll: false, ("Intro", false, "ready"));
        kit.Kit.As(IdentityTestKit.Admin);
        await kit.Kit.UseLinkAsync(videos[0].Id, "https://youtu.be/abcdefgh");
        var current = (await kit.Kit.Catalog.GetBySlugAsync(course.Slug, Ct)).Modules[0];

        CurriculumRequest With(string url) => new(
        [
            new ModuleInput(current.Id, "M", [new SubmoduleInput(current.Submodules[0].Id, "S",
            [
                new SectionInput(current.Submodules[0].Sections[0].Id, "A", [new VideoInput(videos[0].Id, null, "Intro", url, 5, false)], [], []),
                new SectionInput(current.Submodules[0].Sections[1].Id, "B", [], [], []),
            ])]),
        ]);

        await kit.Kit.Admin.SaveCurriculumAsync(course.Id, With("https://youtu.be/abcdefgh"), Ct);
        var changed = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Kit.Admin.SaveCurriculumAsync(course.Id, With("https://youtu.be/zzzzzzzz"), Ct));
        Assert.Equal(CourseAdminService.ReplaceLinkMessage, changed.Message);
    }

    [Fact]
    public async Task Files_in_use_cannot_be_deleted_and_released_files_are_deleted_from_streaming()
    {
        var kit = new MediaKit();
        var (course, videos) = await kit.CourseWithVideosAsync(enroll: false, ("Intro", false, "ready"));
        kit.Kit.As(IdentityTestKit.Admin);
        var oldFile = videos[0].Media!.Id;

        await Assert.ThrowsAsync<ConflictException>(() => kit.Media.DeleteAsync(oldFile, Ct));

        // Replacing the video's file releases the old one.
        var newFile = await kit.Kit.UploadedFileAsync(course.Id);
        var current = (await kit.Kit.Catalog.GetBySlugAsync(course.Slug, Ct)).Modules[0];
        await kit.Kit.Admin.SaveCurriculumAsync(course.Id, new CurriculumRequest(
        [
            new ModuleInput(current.Id, "M", [new SubmoduleInput(current.Submodules[0].Id, "S",
            [
                new SectionInput(current.Submodules[0].Sections[0].Id, "A", [new VideoInput(videos[0].Id, null, "Intro", null, 5, false, newFile)], [], []),
                new SectionInput(current.Submodules[0].Sections[1].Id, "B", [], [], []),
            ])]),
        ]), Ct);
        Assert.Equal(MediaStatusCodes.Deleting, kit.Kit.FileStatus(oldFile));

        Assert.Equal(1, await kit.Deletions.SendPendingAsync(Ct));
        Assert.Equal(0, await kit.Deletions.SendPendingAsync(Ct));
        await kit.Streaming.Received(1).DeleteAsync(kit.AssetIdOf(oldFile), Arg.Any<CancellationToken>());

        // Streaming confirms; deleting the course releases the rest.
        await kit.SendAsync(kit.AssetIdOf(oldFile), "asset.deleted", 5);
        await kit.Kit.Admin.DeleteAsync(course.Id, Ct);
        Assert.Equal((MediaStatusCodes.Deleted, MediaStatusCodes.Deleting), (kit.Kit.FileStatus(oldFile), kit.Kit.FileStatus(newFile)));
    }

    [Fact]
    public async Task Uploaded_videos_count_for_progress_once_ready_and_use_the_measured_length()
    {
        var kit = new MediaKit();
        var (course, videos) = await kit.CourseWithVideosAsync(enroll: true, ("Ready", false, "ready"), ("Busy", false, "processing"));
        var ready = videos.Single(v => v.Title == "Ready");
        var file = await kit.Kit.Identity.Db.MediaAssets.SingleAsync(a => a.Id == ready.Media!.Id);
        file.DurationSeconds = 50;
        await kit.Kit.Identity.Db.SaveChangesAsync();
        kit.Kit.Identity.Db.ChangeTracker.Clear();

        var before = await kit.Kit.Progress.GetCourseAsync(course.Slug, Ct);
        // The browser claims 100 seconds; Streaming measured 50, so 45 seconds is 90%.
        var after = await kit.Kit.Progress.SavePositionAsync(ready.Id, new VideoPositionRequest(45, 100), Ct);

        Assert.Equal(2, before.TotalLessons);
        Assert.Equal((1, 50), (after.CompletedLessons, after.Percent));
        Assert.Equal(50, after.Videos.Single().DurationSeconds);
    }
}
