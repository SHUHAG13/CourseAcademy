using System.Text.Json;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Adds the frontend's demo catalogue (instructors, four courses with their curriculum) when no course exists yet.
/// The data is the frontend's <c>courses.data.ts</c>, exported to <c>demo-courses.json</c>. Course ids 1–4 are kept,
/// because demo records that still live in the browser refer to them.
/// Decisions: video links are kept; documents and file materials are seeded without a file (uploaded later);
/// Link materials keep their link. Demo instructor accounts are linked to their profiles and given course grants.
/// </summary>
public sealed class DemoCourseSeeder(AppDbContext db, ILogger<DemoCourseSeeder> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Which demo account teaches as which instructor profile.</summary>
    private static readonly Dictionary<string, string> InstructorAccounts = new()
    {
        ["tanvir-hasan"] = "instructor@courseacademy.demo",
        ["sabbir-ahmed"] = "sabbir@courseacademy.demo",
    };

    private static readonly string[] InstructorGrantPermissions =
        [PermissionCodes.CoursesView, PermissionCodes.CoursesEdit, PermissionCodes.CommunityModerate, PermissionCodes.CertificatesManage];

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.Courses.IgnoreQueryFilters().AnyAsync(ct)) return;
        var data = await LoadAsync(ct);

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var instructors = await SeedInstructorsAsync(data, ct);
            await SeedCoursesAsync(data, instructors, ct);
            await LinkInstructorAccountsAsync(data, instructors, ct);
            await transaction.CommitAsync(ct);
        });
        logger.LogInformation("Added the demo catalogue: {Courses} courses, {Instructors} instructors.", data.Courses.Count, data.Instructors.Count);
    }

    private async Task<Dictionary<string, Instructor>> SeedInstructorsAsync(SeedData data, CancellationToken ct)
    {
        var map = new Dictionary<string, Instructor>();
        foreach (var i in data.Instructors)
        {
            var instructor = new Instructor
            {
                Name = i.Name, Title = i.Title, Bio = i.Bio, ExperienceYears = i.ExperienceYears, Students = i.Students, Rating = i.Rating,
                Expertise = i.Expertise.Select((e, index) => new InstructorExpertise { Expertise = e, SortOrder = index + 1 }).ToList(),
            };
            db.Instructors.Add(instructor);
            map[i.Id] = instructor;
        }
        await db.SaveChangesAsync(ct);
        return map;
    }

    private async Task SeedCoursesAsync(SeedData data, Dictionary<string, Instructor> instructors, CancellationToken ct)
    {
        var categories = await db.Categories.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var formats = await db.CourseFormats.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var levels = await db.CourseLevels.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var badges = await db.CourseBadges.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var documentTypes = await db.DocumentTypes.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var materialTypes = await db.MaterialTypes.ToDictionaryAsync(c => c.Code, c => c.Id, ct);

        // Course ids are fixed (1–4), so the identity column must accept explicit values for this insert.
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Course] ON", ct);
        foreach (var c in data.Courses)
        {
            db.Courses.Add(new Course
            {
                Id = c.Id, Slug = c.Slug, Title = c.Title, Subtitle = c.Subtitle, Description = c.Description,
                CategoryId = categories[c.Category], CourseFormatId = formats[c.Format], CourseLevelId = levels[c.Level],
                CourseBadgeId = c.Badge is null ? null : badges[c.Badge], InstructorId = instructors[c.InstructorId].Id,
                IsPublished = c.Published, IsFeatured = c.Featured ?? false, Language = c.Language, DurationHours = c.DurationHours,
                OriginalPrice = c.OriginalPrice, Price = c.Price, CoverLabel = c.CoverLabel, CoverIcon = c.CoverIcon, ImageUrl = c.Image,
                Rating = c.Rating, RatingCount = c.RatingCount, Students = c.Students, ContentUpdatedMonth = c.Updated,
            });
        }
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Course] OFF", ct);

        foreach (var c in data.Courses)
        {
            db.CourseOutcomes.AddRange(c.Outcomes.Select((t, i) => new CourseOutcome { CourseId = c.Id, Text = t, SortOrder = i + 1 }));
            db.CourseRequirements.AddRange(c.Requirements.Select((t, i) => new CourseRequirement { CourseId = c.Id, Text = t, SortOrder = i + 1 }));
            db.LiveSessions.AddRange(c.Schedule.Select((s, i) =>
            {
                BangladeshTime.TryParseToUtc(s.StartsAt, out var startsAt);
                return new LiveSession { CourseId = c.Id, Title = s.Title, StartsAtUtc = startsAt, DurationMinutes = s.DurationMinutes, MeetingUrl = s.MeetingUrl, SortOrder = i + 1 };
            }));
            db.Materials.AddRange(c.Materials.Select((m, i) => new Material
            {
                CourseId = c.Id, MaterialTypeId = materialTypes[m.Type], Title = m.Title, SortOrder = i + 1,
                ExternalUrl = m.Type == "Link" ? m.Url : null,
            }));

            foreach (var (m, mi) in c.Modules.Select((m, i) => (m, i)))
            {
                var module = new CourseModule { CourseId = c.Id, Title = m.Title, SortOrder = mi + 1 };
                foreach (var (s, si) in m.Submodules.Select((s, i) => (s, i)))
                {
                    var submodule = new Submodule { Title = s.Title, SortOrder = si + 1 };
                    foreach (var (sec, i) in s.Sections.Select((x, i) => (x, i))) submodule.Sections.Add(BuildSection(c.Id, sec, 0, i, documentTypes));
                    module.Submodules.Add(submodule);
                }
                db.CourseModules.Add(module);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Topics hang off their parent section, so EF inserts them after it and fills in the ids.</summary>
    private static Section BuildSection(int courseId, SeedSection s, int depth, int index, Dictionary<string, int> documentTypes)
    {
        var section = new Section { CourseId = courseId, Depth = depth, Title = s.Title, SortOrder = index + 1 };
        var videos = s.Videos.Select((v, i) => (Key: v.Id, Video: new SectionVideo
        {
            Title = v.Title, ExternalUrl = v.Url, Minutes = v.Minutes, IsPreview = v.Preview, SortOrder = i + 1,
        })).ToList();
        foreach (var (_, video) in videos) section.Videos.Add(video);

        foreach (var (d, i) in s.Documents.Select((d, i) => (d, i)))
        {
            section.Documents.Add(new SectionDocument
            {
                Title = d.Title, DocumentTypeId = documentTypes[d.Type], SortOrder = i + 1,
                Video = videos.FirstOrDefault(v => v.Key == d.VideoId).Video,
            });
        }
        foreach (var (t, i) in s.Topics.Select((t, i) => (t, i)))
        {
            var topic = BuildSection(courseId, t, depth + 1, i, documentTypes);
            topic.ParentSection = section;
            section.Topics.Add(topic);
        }
        return section;
    }

    private async Task LinkInstructorAccountsAsync(SeedData data, Dictionary<string, Instructor> instructors, CancellationToken ct)
    {
        var permissionIds = await db.Permissions.Where(p => InstructorGrantPermissions.Contains(p.Code)).Select(p => p.Id).ToListAsync(ct);
        foreach (var (instructorKey, email) in InstructorAccounts)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null || !instructors.TryGetValue(instructorKey, out var instructor)) continue;
            user.InstructorId = instructor.Id;
            foreach (var course in data.Courses.Where(c => c.InstructorId == instructorKey))
            {
                db.CourseGrants.Add(new CourseGrant
                {
                    UserId = user.Id, CourseId = course.Id,
                    Permissions = permissionIds.Select(id => new CourseGrantPermission { PermissionId = id }).ToList(),
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<SeedData> LoadAsync(CancellationToken ct)
    {
        await using var stream = typeof(DemoCourseSeeder).Assembly.GetManifestResourceStream("CourseAcademy.Api.Data.Seed.demo-courses.json")
            ?? throw new InvalidOperationException("The demo course data is missing from the build.");
        return await JsonSerializer.DeserializeAsync<SeedData>(stream, Json, ct)
            ?? throw new InvalidOperationException("The demo course data could not be read.");
    }

    // Shapes of demo-courses.json (the frontend's model).
    private sealed record SeedData(List<SeedInstructor> Instructors, List<SeedCourse> Courses);
    private sealed record SeedInstructor(string Id, string Name, string Title, string Bio, List<string> Expertise, int ExperienceYears, int Students, decimal Rating);
    private sealed record SeedCourse(
        int Id, string Slug, string Title, string Subtitle, string Description, string Category, string Format, bool Published,
        string InstructorId, decimal Rating, int RatingCount, int Students, int DurationHours, string Level, string Language,
        decimal Price, decimal OriginalPrice, string CoverLabel, string CoverIcon, string? Image, string? Badge, bool? Featured, string Updated,
        List<string> Outcomes, List<string> Requirements, List<SeedModule> Modules, List<SeedSession> Schedule, List<SeedMaterial> Materials);
    private sealed record SeedModule(string Title, List<SeedSubmodule> Submodules);
    private sealed record SeedSubmodule(string Title, List<SeedSection> Sections);
    private sealed record SeedSection(string Title, List<SeedVideo> Videos, List<SeedDocument> Documents, List<SeedSection> Topics);
    private sealed record SeedVideo(string Id, string Title, string Url, int Minutes, bool Preview);
    private sealed record SeedDocument(string Title, string Type, string? VideoId);
    private sealed record SeedSession(string Title, string StartsAt, int DurationMinutes, string MeetingUrl);
    private sealed record SeedMaterial(string Title, string Type, string Url);
}
