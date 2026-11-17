using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Adds a small demo catalogue when no course exists yet (development only): one instructor teaching one published
/// Hybrid course (1 module, 1 submodule, 2 sections, 2 videos, 1 document, 1 live class, 1 material), the instructor
/// account linked to the profile with a course grant, and one paid enrollment for the demo student.
/// Videos are links (older-style videos); the document has no file yet; the material is a link.
/// Runs after <see cref="DemoDataSeeder"/>, which adds the accounts.
/// </summary>
public sealed class DemoCourseSeeder(AppDbContext db, TimeProvider clock, ILogger<DemoCourseSeeder> logger)
{
    private static readonly string[] InstructorGrantPermissions =
        [PermissionCodes.CoursesView, PermissionCodes.CoursesEdit, PermissionCodes.CommunityModerate, PermissionCodes.CertificatesManage];

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.Courses.IgnoreQueryFilters().AnyAsync(ct)) return;
        var instructorAccount = await db.Users.FirstOrDefaultAsync(u => u.Email == DemoDataSeeder.InstructorEmail, ct);
        var student = await db.Users.FirstOrDefaultAsync(u => u.Email == DemoDataSeeder.StudentEmail, ct);
        if (instructorAccount is null || student is null) return;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var instructor = new Instructor
            {
                Name = "Tanvir Hasan", Title = "Senior Video Editor", ExperienceYears = 8, Students = 1200, Rating = 4.8m,
                Bio = "Edits documentaries and brand films, and teaches editing to beginners.",
                Expertise = [new() { Expertise = "Premiere Pro", SortOrder = 1 }, new() { Expertise = "Colour grading", SortOrder = 2 }],
            };
            db.Instructors.Add(instructor);
            await db.SaveChangesAsync(ct);

            var course = await AddCourseAsync(instructor.Id, ct);
            await GrantInstructorAsync(instructorAccount.Id, instructor.Id, course.Id, ct);
            await AddPaidEnrollmentAsync(student, course, ct);

            await transaction.CommitAsync(ct);
        });
        logger.LogInformation("Added the demo catalogue: 1 instructor, 1 course, 1 paid enrollment.");
    }

    private async Task<Course> AddCourseAsync(int instructorId, CancellationToken ct)
    {
        int Lookup<T>(IQueryable<T> set, string code) where T : LookupEntity => set.Single(x => x.Code == code).Id;
        var now = clock.GetUtcNow().UtcDateTime;
        // The live class is a week from now at 20:00 Bangladesh time.
        BangladeshTime.TryParseToUtc(BangladeshTime.FromUtc(now.AddDays(7))[..10] + "T20:00", out var classStartsAt);

        var course = new Course
        {
            Slug = "video-editing-basics", Title = "Video Editing Basics", Subtitle = "Cut, trim and publish your first video",
            Description = "A short, practical start in video editing: learn the workspace, make clean cuts and export for YouTube.",
            CategoryId = Lookup(db.Categories, "video-editing"), CourseFormatId = Lookup(db.CourseFormats, "Hybrid"),
            CourseLevelId = Lookup(db.CourseLevels, "Beginner"), InstructorId = instructorId,
            IsPublished = true, IsFeatured = true, Language = "Bangla", DurationHours = 6,
            OriginalPrice = 3000, Price = 2500, CoverLabel = "Editing", CoverIcon = "bi-camera-reels",
            Rating = 4.8m, RatingCount = 2, Students = 1, ContentUpdatedMonth = now.ToString("yyyy-MM"),
            Outcomes = [new() { Text = "Find your way around an editing workspace", SortOrder = 1 }, new() { Text = "Cut and export a short video", SortOrder = 2 }],
            Requirements = [new() { Text = "A computer with a free video editor installed", SortOrder = 1 }],
            Sessions =
            [
                new() { Title = "Live Q&A: your first edit", StartsAtUtc = classStartsAt, DurationMinutes = 60, MeetingUrl = "https://meet.example.com/video-editing-basics", SortOrder = 1 },
            ],
            Materials =
            [
                new() { Title = "Editing glossary", MaterialTypeId = Lookup(db.MaterialTypes, "Link"), ExternalUrl = "https://example.com/editing-glossary", SortOrder = 1 },
            ],
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);

        var workspace = new SectionVideo { Title = "Tour of the editing workspace", ExternalUrl = "https://example.com/demo-videos/editing-workspace", Minutes = 12, IsPreview = true, SortOrder = 1 };
        var firstCut = new Section
        {
            CourseId = course.Id, Depth = 0, Title = "Your first cut", SortOrder = 2,
            Videos = [new() { Title = "Cutting and trimming clips", ExternalUrl = "https://example.com/demo-videos/cutting-and-trimming", Minutes = 18, SortOrder = 1 }],
        };
        var intro = new Section
        {
            CourseId = course.Id, Depth = 0, Title = "The editing workspace", SortOrder = 1,
            Videos = [workspace],
            Documents = [new() { Title = "Keyboard shortcuts", DocumentTypeId = Lookup(db.DocumentTypes, "PDF"), Video = workspace, SortOrder = 1 }],
        };
        db.CourseModules.Add(new CourseModule
        {
            CourseId = course.Id, Title = "Getting started", SortOrder = 1,
            Submodules = [new() { Title = "First steps", SortOrder = 1, Sections = [intro, firstCut] }],
        });
        await db.SaveChangesAsync(ct);
        return course;
    }

    /// <summary>The instructor account teaches as the profile and may manage the course.</summary>
    private async Task GrantInstructorAsync(int userId, int instructorId, int courseId, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        user.InstructorId = instructorId;
        var permissionIds = await db.Permissions.Where(p => InstructorGrantPermissions.Contains(p.Code)).Select(p => p.Id).ToListAsync(ct);
        db.CourseGrants.Add(new CourseGrant
        {
            UserId = userId, CourseId = courseId,
            Permissions = permissionIds.Select(id => new CourseGrantPermission { PermissionId = id }).ToList(),
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The demo student bought the course three days ago (demo gateway, bKash).</summary>
    private async Task AddPaidEnrollmentAsync(User student, Course course, CancellationToken ct)
    {
        var paidAt = clock.GetUtcNow().UtcDateTime.AddDays(-3);
        var payment = new PaymentTransaction
        {
            TransactionNo = "DEMO-SAMPLE-0001", Gateway = DemoPaymentGateway.GatewayName,
            UserId = student.Id, UserName = student.Name, UserEmail = student.Email,
            CourseId = course.Id, CourseSlug = course.Slug, CourseTitle = course.Title,
            PaymentMethodId = db.PaymentMethods.Single(m => m.Code == "bkash").Id, MaskedAccount = "017•••••000",
            OriginalAmount = course.OriginalPrice, DiscountAmount = course.OriginalPrice - course.Price, Amount = course.Price,
            CurrencyId = LookupIds.Currency("BDT"), PaymentStatusId = LookupIds.PaymentStatus(PaymentStatusCodes.Success),
            ExpiresAt = paidAt.AddMinutes(30), FulfilledAt = paidAt,
        };
        db.PaymentTransactions.Add(payment);
        db.Enrollments.Add(new Enrollment
        {
            UserId = student.Id, CourseId = course.Id, PaymentTransaction = payment, EnrolledAt = paidAt,
            EnrollmentStatusId = LookupIds.EnrollmentStatus(EnrollmentStatusCodes.Active),
        });
        await db.SaveChangesAsync(ct);
        // The save stamps "now"; the demo payment is dated when it was made.
        await db.PaymentTransactions.Where(t => t.Id == payment.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedAt, paidAt), ct);
    }
}
