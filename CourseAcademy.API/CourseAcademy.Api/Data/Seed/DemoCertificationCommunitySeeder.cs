using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Payments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Adds demo certification and community data to the demo course (development only), when that area is still empty:
/// <list type="bullet">
/// <item>a central exam rule on the course ("Final editing project", pass mark 60 of 100, 2 attempts);</item>
/// <item>two more demo students who bought the course and finished every lesson: Sample Learner passed the exam and
/// has certificate CA-YYYY-K7Q2XM; Nusrat Jahan's hand-in waits for review;</item>
/// <item>four posts (a pinned welcome, two questions, one hidden spam post) with comments and replies.</item>
/// </list>
/// The demo student keeps 0% progress, to try the whole flow. Runs after <see cref="DemoCourseSeeder"/>.
/// </summary>
public sealed class DemoCertificationCommunitySeeder(
    AppDbContext db, IPasswordHasher<User> passwordHasher, TimeProvider clock, ILogger<DemoCertificationCommunitySeeder> logger)
{
    public const string SampleLearnerEmail = "sample@courseacademy.demo";
    public const string NusratEmail = "nusrat@courseacademy.demo";
    public const string DemoPassword = "Student@123";
    public const string DemoCertificateSuffix = "K7Q2XM";
    private const string CourseSlug = "video-editing-basics";

    public async Task SeedAsync(CancellationToken ct)
    {
        var course = await db.Courses.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Slug == CourseSlug, ct);
        if (course is null) return;
        if (await db.CertificationRules.IgnoreQueryFilters().AnyAsync(ct)
            || await db.Certificates.IgnoreQueryFilters().AnyAsync(ct)
            || await db.Posts.IgnoreQueryFilters().AnyAsync(ct)) return;
        var instructor = await db.Users.FirstOrDefaultAsync(u => u.Email == DemoDataSeeder.InstructorEmail, ct);
        var student = await db.Users.FirstOrDefaultAsync(u => u.Email == DemoDataSeeder.StudentEmail, ct);
        if (instructor is null || student is null) return;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow().UtcDateTime;
            var dated = new List<(BaseEntity Row, DateTime CreatedAt)>();

            db.CertificationRules.Add(new CertificationRule
            {
                CourseId = course.Id, IsEnabled = true, CertificationModeId = CertificationCodes.ModeId(CertificationCodes.Exam),
                MinProgress = 100, ExamTypeId = CertificationCodes.ExamTypeId(CertificationCodes.Project), ExamTitle = "Final editing project",
                ExamInstructions = "Edit a 60–90 second video from your own clips: clean cuts, music and a title. Upload it to YouTube or Google Drive and share the link.",
                MaxScore = 100, PassingScore = 60, MaxAttempts = 2,
            });

            var sample = await AddLearnerAsync("Sample Learner", SampleLearnerEmail, course, 10, "0002", dated, ct);
            var nusrat = await AddLearnerAsync("Nusrat Jahan", NusratEmail, course, 6, "0003", dated, ct);
            var projectId = CertificationCodes.ExamTypeId(CertificationCodes.Project);

            db.ExamSubmissions.Add(new ExamSubmission
            {
                UserId = sample.Id, CourseId = course.Id, ExamTypeId = projectId,
                Url = "https://drive.google.com/file/d/demo-sample-learner-final-edit", Notes = "Edited in Premiere Pro. Music from the YouTube Audio Library.",
                SubmittedAt = now.AddDays(-4), SubmissionStatusId = CertificationCodes.StatusId(CertificationCodes.Passed),
                Score = 82, Feedback = "Clean cuts and good pacing. Well done!", ReviewedByUserId = instructor.Id, ReviewedAt = now.AddDays(-3),
            });
            db.Certificates.Add(new Certificate
            {
                Code = $"CA-{now.AddDays(-3).Year}-{DemoCertificateSuffix}", UserId = sample.Id, CourseId = course.Id,
                UserName = sample.Name, CourseTitle = course.Title, InstructorName = course.Instructor.Name,
                CertificationModeId = CertificationCodes.ModeId(CertificationCodes.Exam), ExamTypeId = projectId, ScorePercent = 82,
                IssuedAt = now.AddDays(-3),
            });
            db.ExamSubmissions.Add(new ExamSubmission
            {
                UserId = nusrat.Id, CourseId = course.Id, ExamTypeId = projectId,
                Url = "https://www.youtube.com/watch?v=demo-nusrat-final", Notes = "My first edit with music and titles.",
                SubmittedAt = now.AddDays(-1), SubmissionStatusId = CertificationCodes.StatusId(CertificationCodes.Submitted),
            });
            await db.SaveChangesAsync(ct);

            await AddCommunityAsync(course.Id, instructor.Id, student.Id, sample.Id, nusrat.Id, now, dated, ct);

            // Saves stamp "now"; the demo records are dated in the past.
            foreach (var (row, createdAt) in dated) await SetCreatedAtAsync(row, createdAt, ct);
            await transaction.CommitAsync(ct);
        });
        logger.LogInformation("Added the demo certification and community data: 1 rule, 2 learners, 2 hand-ins, 1 certificate, 4 posts.");
    }

    /// <summary>A demo student who bought the course <paramref name="daysAgo"/> days ago and completed every lesson.</summary>
    private async Task<User> AddLearnerAsync(
        string name, string email, Course course, int daysAgo, string paymentNo, List<(BaseEntity, DateTime)> dated, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            user = new User { Name = name, Email = email, RoleId = AccessSeed.StudentRoleId };
            user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }

        var paidAt = clock.GetUtcNow().UtcDateTime.AddDays(-daysAgo);
        var payment = new PaymentTransaction
        {
            TransactionNo = $"DEMO-SAMPLE-{paymentNo}", Gateway = DemoPaymentGateway.GatewayName,
            UserId = user.Id, UserName = user.Name, UserEmail = user.Email,
            CourseId = course.Id, CourseSlug = course.Slug, CourseTitle = course.Title,
            PaymentMethodId = db.PaymentMethods.Single(m => m.Code == "nagad").Id, MaskedAccount = "018•••••000",
            OriginalAmount = course.OriginalPrice, DiscountAmount = course.OriginalPrice - course.Price, Amount = course.Price,
            CurrencyId = LookupIds.Currency("BDT"), PaymentStatusId = LookupIds.PaymentStatus(PaymentStatusCodes.Success),
            ExpiresAt = paidAt.AddMinutes(30), FulfilledAt = paidAt,
        };
        db.PaymentTransactions.Add(payment);
        if (!await db.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == course.Id, ct))
        {
            db.Enrollments.Add(new Enrollment
            {
                UserId = user.Id, CourseId = course.Id, PaymentTransaction = payment, EnrolledAt = paidAt,
                EnrollmentStatusId = LookupIds.EnrollmentStatus(EnrollmentStatusCodes.Active),
            });
        }

        // Every lesson done: each video, and each section without videos (BR-LRN-15).
        var sections = await db.Sections.Include(s => s.Videos).Where(s => s.CourseId == course.Id).ToListAsync(ct);
        var completedAt = paidAt.AddDays(daysAgo / 2.0);
        foreach (var section in sections)
        {
            IEnumerable<int?> lessons = section.Videos.Count == 0 ? [null] : section.Videos.Select(v => (int?)v.Id);
            foreach (var videoId in lessons)
            {
                db.LessonProgress.Add(new LessonProgress
                {
                    UserId = user.Id, CourseId = course.Id, SectionId = section.Id, SectionVideoId = videoId,
                    IsCompleted = true, CompletedAt = completedAt,
                });
            }
        }
        await db.SaveChangesAsync(ct);
        dated.Add((payment, paidAt));
        return user;
    }

    private async Task AddCommunityAsync(
        int courseId, int instructorId, int studentId, int sampleId, int nusratId, DateTime now, List<(BaseEntity, DateTime)> dated, CancellationToken ct)
    {
        Post AddPost(int authorId, double hoursAgo, string title, string body, bool pinned = false, bool hidden = false)
        {
            var post = new Post { CourseId = courseId, AuthorUserId = authorId, Title = title, Body = body, IsPinned = pinned, IsHidden = hidden };
            db.Posts.Add(post);
            dated.Add((post, now.AddHours(-hoursAgo)));
            return post;
        }

        var welcome = AddPost(instructorId, 200, "Welcome to the course community!",
            "Introduce yourself here: where you are from and what you want to edit. Ask questions about any lesson, and help each other out. Please keep it friendly and on topic.",
            pinned: true);
        var frameRate = AddPost(studentId, 30, "Which frame rate should I edit in?",
            "My phone records at 30 fps but some YouTube tutorials say 24 fps looks more like a film. Which one should I choose for my project?");
        var export = AddPost(sampleId, 48, "Best export settings for YouTube?",
            "My exported videos look blurry after uploading. Which settings do you recommend?");
        AddPost(sampleId, 5, "Cheap followers for sale", "Message me for cheap social media followers.", hidden: true);
        await db.SaveChangesAsync(ct);

        async Task<Comment> AddComment(Post post, int authorId, double hoursAgo, string body, Comment? parent = null)
        {
            var comment = new Comment { PostId = post.Id, CourseId = courseId, ParentCommentId = parent?.Id, AuthorUserId = authorId, Body = body };
            db.Comments.Add(comment);
            await db.SaveChangesAsync(ct);
            dated.Add((comment, now.AddHours(-hoursAgo)));
            return comment;
        }

        await AddComment(welcome, nusratId, 150, "Hello from Chattogram! I want to edit travel videos for my channel.");
        var question = await AddComment(frameRate, instructorId, 26,
            "Edit in the frame rate you recorded in. Changing 30 fps footage to 24 fps makes movement look jumpy.");
        await AddComment(frameRate, studentId, 25, "That makes sense, thank you!", question);
        await AddComment(frameRate, sampleId, 20, "The workspace lesson shows where to check the sequence settings, too.");
        var tip = await AddComment(export, nusratId, 40, "Export in the same resolution you edited in, and use a higher bitrate.");
        await AddComment(export, instructorId, 38, "Right. H.264, 1080p and about 16 Mbps works well for YouTube.", tip);
    }

    private Task<int> SetCreatedAtAsync(BaseEntity row, DateTime createdAt, CancellationToken ct) => row switch
    {
        Post => SetCreatedAtAsync(db.Posts, row.Id, createdAt, ct),
        Comment => SetCreatedAtAsync(db.Comments, row.Id, createdAt, ct),
        PaymentTransaction => SetCreatedAtAsync(db.PaymentTransactions, row.Id, createdAt, ct),
        _ => throw new InvalidOperationException($"No demo date for {row.GetType().Name}."),
    };

    private static Task<int> SetCreatedAtAsync<T>(DbSet<T> set, int id, DateTime createdAt, CancellationToken ct) where T : BaseEntity =>
        set.Where(e => e.Id == id).ExecuteUpdateAsync(s => s.SetProperty(e => e.CreatedAt, createdAt), ct);
}
