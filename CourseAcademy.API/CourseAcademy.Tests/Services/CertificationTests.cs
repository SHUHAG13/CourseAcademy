using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Services;

/// <summary>Certification rules, the central exam and certificates (BR-CERT-01 to 17).</summary>
public class CertificationTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static RuleSaveRequest ExamRule(int maxAttempts = 2, int passingScore = 60) =>
        new(true, CertificationCodes.Exam, 100, CertificationCodes.Project, " Final project ", " Share the link. ", 100, passingScore, maxAttempts);

    /// <summary>
    /// A free published course with two lessons (two sections without videos). The demo student and the learner
    /// are enrolled; the demo student is signed in afterwards.
    /// </summary>
    private static async Task<(CatalogTests.Kit Kit, CourseDetailDto Course)> SetUp()
    {
        var kit = new CatalogTests.Kit().As(IdentityTestKit.Admin);
        var created = await kit.Admin.CreateAsync(kit.Course("free-course") with { OriginalPrice = 0, Price = 0 }, Ct);
        await kit.Admin.SaveCurriculumAsync(created.Id, CatalogTests.TwoSections(), Ct);
        var course = await kit.Admin.SetPublishedAsync(created.Id, true, Ct);
        kit.As(IdentityTestKit.Learner);
        await kit.Enrollments.EnrollFreeAsync(course.Slug, Ct);
        kit.As(IdentityTestKit.Student);
        await kit.Enrollments.EnrollFreeAsync(course.Slug, Ct);
        return (kit, course);
    }

    private static async Task CompleteAllAsync(CatalogTests.Kit kit, CourseDetailDto course)
    {
        foreach (var section in course.Modules[0].Submodules[0].Sections) await kit.Progress.SetSectionCompletedAsync(section.Id, true, Ct);
    }

    private static int FirstSection(CourseDetailDto course) => course.Modules[0].Submodules[0].Sections[0].Id;

    [Fact]
    public async Task Without_a_saved_rule_finishing_the_course_issues_a_completion_certificate_by_itself()
    {
        var (kit, course) = await SetUp();

        var before = await kit.Certification.GetStatusAsync(course.Id, Ct);
        Assert.Equal((CertificationCodes.Completion, 100, 0, false), (before.Rule.ModeCode, before.Rule.MinProgress, before.Progress, before.ProgressMet));
        Assert.Null(await kit.Certification.ClaimAsync(course.Id, Ct));

        await CompleteAllAsync(kit, course);
        var status = await kit.Certification.GetStatusAsync(course.Id, Ct);

        var certificate = Assert.IsType<CertificateDto>(status.Certificate);
        Assert.Matches($"^CA-{TestDb.Start.Year}-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{{6}}$", certificate.Code);
        Assert.Equal(("Demo Student", "Photography Basics", "Tanvir Hasan", CertificationCodes.Completion, (int?)null),
            (certificate.UserName, certificate.CourseTitle, certificate.InstructorName, certificate.ModeCode, certificate.ScorePercent));
        // Claiming again returns the same certificate; undoing a lesson does not take it away (BR-CERT-03).
        Assert.Equal(certificate.Code, (await kit.Certification.ClaimAsync(course.Id, Ct))!.Code);
        await kit.Progress.SetSectionCompletedAsync(FirstSection(course), false, Ct);
        await kit.Progress.SetSectionCompletedAsync(FirstSection(course), true, Ct);
        Assert.Single(await kit.Certification.GetMineAsync(Ct));
    }

    [Fact]
    public async Task A_revoked_certificate_is_not_issued_again_until_it_is_restored()
    {
        var (kit, course) = await SetUp();
        await CompleteAllAsync(kit, course);
        var certificate = (await kit.Certification.GetMineAsync(Ct))[0];

        kit.As(IdentityTestKit.Admin);
        var revoked = await kit.Certification.RevokeAsync(certificate.Id, new RevokeCertificateRequest("  Copied work  "), Ct);
        Assert.Equal((true, "Copied work"), (revoked.IsRevoked, revoked.RevokedReason));
        var verified = await kit.Certification.VerifyAsync($"  {certificate.Code.ToLowerInvariant()} ", Ct);
        Assert.Equal((true, "Copied work"), (verified.IsRevoked, verified.RevokedReason));

        // BR-CERT-17: neither more progress nor "Get my certificate" issues a new one.
        kit.As(IdentityTestKit.Student);
        await kit.Progress.SetSectionCompletedAsync(FirstSection(course), false, Ct);
        await kit.Progress.SetSectionCompletedAsync(FirstSection(course), true, Ct);
        Assert.Null(await kit.Certification.ClaimAsync(course.Id, Ct));
        var status = await kit.Certification.GetStatusAsync(course.Id, Ct);
        Assert.Null(status.Certificate);
        Assert.Equal(certificate.Code, status.RevokedCertificate?.Code);

        kit.As(IdentityTestKit.Admin);
        var restored = await kit.Certification.RestoreAsync(certificate.Id, Ct);
        Assert.Equal((false, (string?)null), (restored.IsRevoked, restored.RevokedReason));
        Assert.Single(await kit.Identity.Db.Certificates.ToListAsync());
    }

    [Fact]
    public async Task The_central_exam_is_handed_in_reviewed_and_a_pass_issues_an_exam_certificate()
    {
        var (kit, course) = await SetUp();
        kit.As(IdentityTestKit.Admin);
        await kit.Certification.SaveRuleAsync(course.Id, ExamRule(), Ct);

        kit.As(IdentityTestKit.Student);
        var early = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SubmitExamAsync(course.Id, new("https://example.com/work", null), Ct));
        Assert.Equal("Complete the course before the central exam.", early.Message);

        await CompleteAllAsync(kit, course);
        // An exam rule issues nothing on completion.
        Assert.Empty(await kit.Certification.GetMineAsync(Ct));
        var badLink = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SubmitExamAsync(course.Id, new("http://example.com/work", null), Ct));
        Assert.Equal("Enter a link that starts with https://.", badLink.Message);
        var tooLong = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            kit.Certification.SubmitExamAsync(course.Id, new("https://example.com/work", new string('n', 1001)), Ct));
        Assert.Equal("This is too long. Please shorten it.", tooLong.Message);

        var first = await kit.Certification.SubmitExamAsync(course.Id, new(" https://example.com/work ", "  " + new string('n', 1000) + "  "), Ct);
        Assert.Equal((CertificationCodes.Submitted, "https://example.com/work", "Final project", 1000), (first.StatusCode, first.Url, first.ExamTitle, first.Notes.Length));
        var waiting = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SubmitExamAsync(course.Id, new("https://example.com/again", null), Ct));
        Assert.Equal("You cannot hand in the exam right now.", waiting.Message);

        // Reviewing needs certificates.manage on the course.
        kit.As(IdentityTestKit.Instructor);
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Certification.ReviewAsync(first.Id, new(80, null), Ct));
        kit.As(IdentityTestKit.Admin);
        await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.ReviewAsync(first.Id, new(100.5m, null), Ct));
        var failed = await kit.Certification.ReviewAsync(first.Id, new(55.5m, " Add music. "), Ct);
        Assert.Equal((CertificationCodes.Failed, 55.5m, "Add music.", "Academy Admin"),
            (failed.Submission.StatusCode, failed.Submission.Score, failed.Submission.Feedback, failed.Submission.ReviewedBy?.Name));
        Assert.Null(failed.Certificate);
        await Assert.ThrowsAsync<ConflictException>(() => kit.Certification.ReviewAsync(first.Id, new(90, null), Ct));

        kit.As(IdentityTestKit.Student);
        var retry = await kit.Certification.GetStatusAsync(course.Id, Ct);
        Assert.Equal((1, true), (retry.AttemptsLeft, retry.CanSubmit));
        var second = await kit.Certification.SubmitExamAsync(course.Id, new("https://example.com/v2", null), Ct);

        kit.As(IdentityTestKit.Admin);
        var passed = await kit.Certification.ReviewAsync(second.Id, new(69.5m, null), Ct);
        var certificate = Assert.IsType<CertificateDto>(passed.Certificate);
        Assert.Equal((CertificationCodes.Exam, CertificationCodes.Project, 70), (certificate.ModeCode, certificate.ExamTypeCode, certificate.ScorePercent));

        kit.As(IdentityTestKit.Student);
        var done = await kit.Certification.GetStatusAsync(course.Id, Ct);
        Assert.Equal((0, false, certificate.Code), (done.AttemptsLeft, done.CanSubmit, done.Certificate?.Code));
        var noAttempts = await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SubmitExamAsync(course.Id, new("https://example.com/v3", null), Ct));
        Assert.Equal("You have used all your attempts.", noAttempts.Message);
    }

    [Fact]
    public async Task Rules_are_validated_and_only_managers_may_save_them()
    {
        var (kit, course) = await SetUp();

        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Certification.SaveRuleAsync(course.Id, ExamRule(), Ct));
        kit.As(IdentityTestKit.Admin);
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Certification.GetRuleAsync(999_999, Ct));
        Assert.Equal("Give the central exam a title.",
            (await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SaveRuleAsync(course.Id, ExamRule() with { ExamTitle = "  " }, Ct))).Message);
        Assert.Equal("The pass mark must be between 1 and the maximum score.",
            (await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SaveRuleAsync(course.Id, ExamRule(passingScore: 101), Ct))).Message);
        Assert.Equal("Required progress must be between 1 and 100%.",
            (await Assert.ThrowsAsync<BusinessRuleException>(() => kit.Certification.SaveRuleAsync(course.Id, ExamRule() with { MinProgress = 0 }, Ct))).Message);

        var saved = await kit.Certification.SaveRuleAsync(course.Id, ExamRule(maxAttempts: 3, passingScore: 70), Ct);
        Assert.Equal(("Final project", "Share the link.", 70, 3), (saved.ExamTitle, saved.ExamInstructions, saved.PassingScore, saved.MaxAttempts));

        // Completion mode does not check exam fields; invalid ones keep their stored values (BR-CERT-02).
        var completion = await kit.Certification.SaveRuleAsync(course.Id,
            ExamRule(maxAttempts: -1, passingScore: 0) with { ModeCode = CertificationCodes.Completion, ExamTitle = "", MinProgress = 50 }, Ct);
        Assert.Equal((CertificationCodes.Completion, 50, "Final project", 70, 3),
            (completion.ModeCode, completion.MinProgress, completion.ExamTitle, completion.PassingScore, completion.MaxAttempts));
        Assert.Single(await kit.Identity.Db.CertificationRules.ToListAsync());
    }

    [Fact]
    public async Task Managers_see_only_the_submissions_and_certificates_of_their_courses()
    {
        var (kit, course) = await SetUp();
        await CompleteAllAsync(kit, course);
        kit.As(IdentityTestKit.Admin);
        var other = await kit.Admin.CreateAsync(kit.Course("other-course"), Ct);

        kit.As(IdentityTestKit.Instructor);
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Certification.GetCertificatesAsync(new CertificateListQuery(), Ct));

        kit.As(IdentityTestKit.Admin);
        await kit.Identity.Access.SetCourseGrantAsync(kit.Identity.UserId(IdentityTestKit.Instructor), other.Id, new SetCourseGrantRequest(["certificates.manage"]), Ct);
        kit.As(IdentityTestKit.Instructor);
        Assert.Equal(0, (await kit.Certification.GetCertificatesAsync(new CertificateListQuery(), Ct)).Total);

        kit.As(IdentityTestKit.Admin);
        var all = await kit.Certification.GetCertificatesAsync(new CertificateListQuery { Search = " Demo Stu " }, Ct);
        Assert.Equal("Demo Student", Assert.Single(all.Items).UserName);
        Assert.Equal(0, (await kit.Certification.GetSubmissionsAsync(new SubmissionListQuery(), Ct)).Total);
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Certification.VerifyAsync("CA-2000-AAAAAA", Ct));
    }
}
