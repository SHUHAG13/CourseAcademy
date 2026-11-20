using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Services;

/// <summary>Lookup codes of the certification module, and their fixed seed ids.</summary>
public static class CertificationCodes
{
    public const string Completion = "completion";
    public const string Exam = "exam";
    public const string Project = "project";
    public const string Presentation = "presentation";
    public const string Submitted = "submitted";
    public const string Passed = "passed";
    public const string Failed = "failed";

    public static int ModeId(string code) => LookupSeed.CertificationModes.Single(s => s.Code == code).Id;
    public static string ModeCode(int id) => LookupSeed.CertificationModes.Single(s => s.Id == id).Code;
    public static int ExamTypeId(string code) => LookupSeed.ExamTypes.Single(s => s.Code == code).Id;
    public static string ExamTypeCode(int id) => LookupSeed.ExamTypes.Single(s => s.Id == id).Code;
    public static int StatusId(string code) => LookupSeed.SubmissionStatuses.Single(s => s.Code == code).Id;
    public static string StatusCode(int id) => LookupSeed.SubmissionStatuses.Single(s => s.Id == id).Code;

    public static bool IsMode(string? code) => LookupSeed.CertificationModes.Any(s => s.Code == code);
    public static bool IsExamType(string? code) => LookupSeed.ExamTypes.Any(s => s.Code == code);
    public static bool IsStatus(string? code) => LookupSeed.SubmissionStatuses.Any(s => s.Code == code);
}

/// <summary>Certification rules, the central exam, issuing, revoking and verifying certificates (BR-CERT-01 to 17).</summary>
public interface ICertificationService
{
    Task<RuleDto> GetRuleAsync(int courseId, CancellationToken ct);
    Task<RuleDto> SaveRuleAsync(int courseId, RuleSaveRequest request, CancellationToken ct);

    /// <summary>The signed-in learner's certificate status in one course.</summary>
    Task<CertificationStatusDto> GetStatusAsync(int courseId, CancellationToken ct);

    /// <summary>"Get my certificate": issues the completion certificate when the requirement is met. Null when it is not.</summary>
    Task<CertificateDto?> ClaimAsync(int courseId, CancellationToken ct);

    Task<SubmissionDto> SubmitExamAsync(int courseId, ExamSubmitRequest request, CancellationToken ct);

    /// <summary>The signed-in user's certificates, newest first, revoked ones included.</summary>
    Task<IReadOnlyList<CertificateDto>> GetMineAsync(CancellationToken ct);

    Task<PagedResult<SubmissionDto>> GetSubmissionsAsync(SubmissionListQuery query, CancellationToken ct);
    Task<ReviewResultDto> ReviewAsync(int submissionId, ReviewSubmissionRequest request, CancellationToken ct);
    Task<PagedResult<CertificateDto>> GetCertificatesAsync(CertificateListQuery query, CancellationToken ct);
    Task<CertificateDto> RevokeAsync(int certificateId, RevokeCertificateRequest request, CancellationToken ct);
    Task<CertificateDto> RestoreAsync(int certificateId, CancellationToken ct);

    /// <summary>Public lookup by code; trimmed and not case-sensitive (BR-CERT-13).</summary>
    Task<VerifyCertificateDto> VerifyAsync(string code, CancellationToken ct);

    /// <summary>
    /// Called after a lesson is completed: issues the completion certificate when the requirement is now met
    /// (BR-CERT-04). Adds to the change tracker and saves; never fails because a rule is not met.
    /// </summary>
    Task IssueForProgressAsync(int userId, int courseId, CancellationToken ct);
}

public sealed partial class CertificationService(
    ICertificationRuleRepository rules,
    IExamSubmissionRepository submissions,
    ICertificateRepository certificates,
    ICourseRepository courses,
    ILessonProgressRepository progress,
    IEnrollmentRepository enrollments,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : ICertificationService
{
    public const string DefaultExamTitle = "Final project";
    public const int MaxInstructionsLength = 8000;
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const string TooLongMessage = "This is too long. Please shorten it.";
    private const string NotEnrolledMessage = "Enroll in this course to earn its certificate.";
    private const string AlreadyReviewedMessage = "This submission has already been reviewed.";
    private const string AlreadyValidMessage = "This student already has a valid certificate for the course.";

    [GeneratedRegex(@"^https://\S+\.\S+$", RegexOptions.IgnoreCase)]
    private static partial Regex HttpsLink();

    // ── Rules ───────────────────────────────────────────────────

    public async Task<RuleDto> GetRuleAsync(int courseId, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CertificatesManage, courseId, ct);
        await GetCourseAsync(courseId, ct);
        return ToDto(await RuleOrDefaultAsync(courseId, ct));
    }

    public async Task<RuleDto> SaveRuleAsync(int courseId, RuleSaveRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CertificatesManage, courseId, ct);
        await GetCourseAsync(courseId, ct);

        if (!CertificationCodes.IsMode(request.ModeCode)) throw new BusinessRuleException("Choose how the certificate is earned.", "modeCode");
        if (request.MinProgress is < 1 or > 100) throw new BusinessRuleException("Required progress must be between 1 and 100%.", "minProgress");
        if (!CertificationCodes.IsExamType(request.ExamTypeCode)) throw new BusinessRuleException("Choose the type of central exam.", "examTypeCode");
        var title = request.ExamTitle?.Trim() ?? "";
        var instructions = request.ExamInstructions?.Trim() ?? "";
        if (title.Length > 200) throw new BusinessRuleException(TooLongMessage, "examTitle");
        if (instructions.Length > MaxInstructionsLength) throw new BusinessRuleException(TooLongMessage, "examInstructions");

        var titleOk = title.Length > 0;
        var scoresOk = request.MaxScore >= 1 && request.PassingScore >= 1 && request.PassingScore <= request.MaxScore;
        var attemptsOk = request.MaxAttempts >= 0;
        var exam = request.ModeCode == CertificationCodes.Exam;
        // Exam fields are checked only for the exam mode (BR-CERT-02).
        if (exam)
        {
            if (!titleOk) throw new BusinessRuleException("Give the central exam a title.", "examTitle");
            if (request.MaxScore < 1) throw new BusinessRuleException("The maximum score must be 1 or more.", "maxScore");
            if (!scoresOk) throw new BusinessRuleException("The pass mark must be between 1 and the maximum score.", "passingScore");
            if (!attemptsOk) throw new BusinessRuleException("Attempts must be 0 (unlimited) or more.", "maxAttempts");
        }

        var rule = await rules.GetForCourseAsync(courseId, ct);
        if (rule is null)
        {
            rule = DefaultRule(courseId);
            await rules.AddAsync(rule, ct);
        }
        rule.IsEnabled = request.IsEnabled;
        rule.CertificationModeId = CertificationCodes.ModeId(request.ModeCode);
        rule.MinProgress = request.MinProgress;
        rule.ExamTypeId = CertificationCodes.ExamTypeId(request.ExamTypeCode);
        rule.ExamInstructions = instructions.Length == 0 ? null : instructions;
        // In completion mode an exam field that is not valid keeps its stored value.
        if (titleOk) rule.ExamTitle = title;
        if (scoresOk)
        {
            rule.MaxScore = request.MaxScore;
            rule.PassingScore = request.PassingScore;
        }
        if (attemptsOk) rule.MaxAttempts = request.MaxAttempts;

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            throw new ConflictException("This rule was changed in the meantime. Reload the page and try again.");
        }
        return ToDto(rule);
    }

    // ── Students ────────────────────────────────────────────────

    public async Task<CertificationStatusDto> GetStatusAsync(int courseId, CancellationToken ct)
    {
        var learner = await EnsureLearnerAsync(courseId, ct);
        return await BuildStatusAsync(learner, ct);
    }

    public async Task<CertificateDto?> ClaimAsync(int courseId, CancellationToken ct)
    {
        var learner = await EnsureLearnerAsync(courseId, ct);
        var owned = await certificates.ListForUserCourseAsync(learner.Access.UserId, courseId, ct);
        var valid = owned.FirstOrDefault(c => !c.IsRevoked);
        if (valid is not null) return ToDto(valid);
        if (!learner.IsEnrolled) return null;

        var rule = await RuleOrDefaultAsync(courseId, ct);
        if (!IsCompletionRule(rule) || owned.Count > 0) return null;
        if (await ProgressPercentAsync(learner.Access.UserId, courseId, ct) < rule.MinProgress) return null;

        var issued = await IssueAsync(learner.Access.UserId, learner.Course, CertificationCodes.Completion, null, null, ct);
        return await TrySaveIssuedAsync(ct) ? ToDto(issued) : await ValidDtoAsync(learner.Access.UserId, courseId, ct);
    }

    public async Task IssueForProgressAsync(int userId, int courseId, CancellationToken ct)
    {
        var enrollment = await enrollments.GetForUserCourseAsync(userId, courseId, ct);
        if (enrollment?.Status.Code != EnrollmentStatusCodes.Active) return;
        var rule = await RuleOrDefaultAsync(courseId, ct);
        if (!IsCompletionRule(rule)) return;
        // A valid certificate is already there; a revoked one is only made valid again by Restore (BR-CERT-17).
        if (await certificates.AnyAsync(c => c.UserId == userId && c.CourseId == courseId, ct)) return;
        if (await ProgressPercentAsync(userId, courseId, ct) < rule.MinProgress) return;
        var course = (await courses.ListSummariesAsync(c => c.Id == courseId, ct)).FirstOrDefault();
        if (course is null) return;

        await IssueAsync(userId, course, CertificationCodes.Completion, null, null, ct);
        await TrySaveIssuedAsync(ct);
    }

    public async Task<SubmissionDto> SubmitExamAsync(int courseId, ExamSubmitRequest request, CancellationToken ct)
    {
        var learner = await EnsureLearnerAsync(courseId, ct);
        if (!learner.IsEnrolled) throw new ForbiddenException(NotEnrolledMessage);
        var status = await BuildStatusAsync(learner, ct);
        if (!status.CanSubmit)
        {
            throw new BusinessRuleException(
                !status.ProgressMet ? "Complete the course before the central exam."
                : status.AttemptsLeft == 0 ? "You have used all your attempts."
                : "You cannot hand in the exam right now.");
        }

        var url = request.Url?.Trim() ?? "";
        if (!HttpsLink().IsMatch(url)) throw new BusinessRuleException("Enter a link that starts with https://.", "url");
        if (url.Length > 2048) throw new BusinessRuleException(TooLongMessage, "url");
        var notes = request.Notes?.Trim() ?? "";
        if (notes.Length > 1000) throw new BusinessRuleException(TooLongMessage, "notes");

        var submission = new ExamSubmission
        {
            UserId = learner.Access.UserId, CourseId = courseId, ExamTypeId = CertificationCodes.ExamTypeId(status.Rule.ExamTypeCode),
            Url = url, Notes = notes.Length == 0 ? null : notes, SubmittedAt = clock.GetUtcNow().UtcDateTime,
            SubmissionStatusId = CertificationCodes.StatusId(CertificationCodes.Submitted),
        };
        await submissions.AddAsync(submission, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ToDtosAsync([submission], ct))[0];
    }

    public async Task<IReadOnlyList<CertificateDto>> GetMineAsync(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var mine = await certificates.ListQuery().Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt).ThenByDescending(c => c.Id).ToListAsync(ct);
        return mine.Select(ToDto).ToList();
    }

    // ── Instructors and admins ──────────────────────────────────

    public async Task<PagedResult<SubmissionDto>> GetSubmissionsAsync(SubmissionListQuery query, CancellationToken ct)
    {
        var managed = await ManagedCoursesAsync(ct);
        var status = string.IsNullOrWhiteSpace(query.Status) ? CertificationCodes.Submitted : query.Status.Trim();
        if (status != "all" && !CertificationCodes.IsStatus(status)) throw new BusinessRuleException("Choose a valid status.", "status");

        var list = submissions.ListQuery();
        if (managed is not null) list = list.Where(s => managed.Contains(s.CourseId));
        if (query.CourseId is { } courseId) list = list.Where(s => s.CourseId == courseId);
        if (status != "all")
        {
            var statusId = CertificationCodes.StatusId(status);
            list = list.Where(s => s.SubmissionStatusId == statusId);
        }

        var total = await list.CountAsync(ct);
        var page = await list.OrderByDescending(s => s.SubmittedAt).ThenByDescending(s => s.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<SubmissionDto>(await ToDtosAsync(page, ct), total, query.Page, query.PageSize);
    }

    public async Task<ReviewResultDto> ReviewAsync(int submissionId, ReviewSubmissionRequest request, CancellationToken ct)
    {
        var submission = await submissions.GetByIdAsync(submissionId, ct) ?? throw new NotFoundException("Submission not found.");
        await currentUser.EnsureCanAsync(PermissionCodes.CertificatesManage, submission.CourseId, ct);
        if (CertificationCodes.StatusCode(submission.SubmissionStatusId) != CertificationCodes.Submitted) throw new ConflictException(AlreadyReviewedMessage);

        var rule = await RuleOrDefaultAsync(submission.CourseId, ct);
        if (request.Score < 0 || request.Score > rule.MaxScore || decimal.Round(request.Score, 2) != request.Score)
        {
            throw new BusinessRuleException("Enter a score between 0 and the maximum score.", "score");
        }
        var feedback = request.Feedback?.Trim() ?? "";
        if (feedback.Length > 1000) throw new BusinessRuleException(TooLongMessage, "feedback");

        var passed = request.Score >= rule.PassingScore;
        submission.SubmissionStatusId = CertificationCodes.StatusId(passed ? CertificationCodes.Passed : CertificationCodes.Failed);
        submission.Score = request.Score;
        submission.Feedback = feedback.Length == 0 ? null : feedback;
        submission.ReviewedByUserId = currentUser.RequireUserId();
        submission.ReviewedAt = clock.GetUtcNow().UtcDateTime;

        // A pass issues the certificate at once (BR-CERT-10), unless the student already has one (BR-CERT-03)
        // or theirs was revoked (BR-CERT-17).
        Certificate? certificate = null;
        var course = (await courses.ListSummariesAsync(c => c.Id == submission.CourseId, ct)).FirstOrDefault();
        if (passed && course is not null)
        {
            var owned = await certificates.ListForUserCourseAsync(submission.UserId, submission.CourseId, ct);
            certificate = owned.FirstOrDefault(c => !c.IsRevoked);
            if (owned.Count == 0)
            {
                var percent = (int)Math.Round(request.Score / rule.MaxScore * 100m, MidpointRounding.AwayFromZero);
                certificate = await IssueAsync(submission.UserId, course, CertificationCodes.Exam, submission.ExamTypeId, percent, ct);
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(AlreadyReviewedMessage);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            throw new ConflictException(AlreadyReviewedMessage);
        }
        return new ReviewResultDto((await ToDtosAsync([submission], ct))[0], certificate is null ? null : ToDto(certificate));
    }

    public async Task<PagedResult<CertificateDto>> GetCertificatesAsync(CertificateListQuery query, CancellationToken ct)
    {
        var managed = await ManagedCoursesAsync(ct);
        var list = certificates.ListQuery();
        if (managed is not null) list = list.Where(c => managed.Contains(c.CourseId));
        if (query.CourseId is { } courseId) list = list.Where(c => c.CourseId == courseId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var code = term.ToUpperInvariant();
            list = list.Where(c => c.Code.Contains(code) || c.UserName.Contains(term) || c.CourseTitle.Contains(term));
        }

        var total = await list.CountAsync(ct);
        var page = await list.OrderByDescending(c => c.IssuedAt).ThenByDescending(c => c.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<CertificateDto>(page.Select(ToDto).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<CertificateDto> RevokeAsync(int certificateId, RevokeCertificateRequest request, CancellationToken ct)
    {
        var certificate = await GetManagedCertificateAsync(certificateId, ct);
        var reason = request.Reason?.Trim() ?? "";
        if (reason.Length > 200) throw new BusinessRuleException(TooLongMessage, "reason");
        certificate.IsRevoked = true;
        certificate.RevokedReason = reason.Length == 0 ? null : reason;
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(certificate);
    }

    public async Task<CertificateDto> RestoreAsync(int certificateId, CancellationToken ct)
    {
        var certificate = await GetManagedCertificateAsync(certificateId, ct);
        if (!certificate.IsRevoked) return ToDto(certificate);
        if (await certificates.AnyAsync(c => c.UserId == certificate.UserId && c.CourseId == certificate.CourseId && !c.IsRevoked && c.Id != certificate.Id, ct))
        {
            throw new ConflictException(AlreadyValidMessage);
        }
        certificate.IsRevoked = false;
        certificate.RevokedReason = null;
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            throw new ConflictException(AlreadyValidMessage);
        }
        return ToDto(certificate);
    }

    public async Task<VerifyCertificateDto> VerifyAsync(string code, CancellationToken ct)
    {
        var normalised = code.Trim().ToUpperInvariant();
        var c = (normalised.Length == 0 ? null : await certificates.GetByCodeAsync(normalised, ct))
            ?? throw new NotFoundException("Certificate not found.");
        return new VerifyCertificateDto(c.Code, c.IsRevoked, c.RevokedReason, c.UserName, c.CourseTitle,
            CertificationCodes.ModeCode(c.CertificationModeId), c.ExamTypeId is { } type ? CertificationCodes.ExamTypeCode(type) : null,
            c.ScorePercent, c.InstructorName, c.IssuedAt);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private sealed record Learner(UserAccess Access, CourseSummary Course, bool IsEnrolled);

    /// <summary>
    /// Like learning (BR-LRN-02): needs <c>learn.view</c> and course access — an active enrollment, or managing the
    /// course. Only an active enrollment can earn a certificate (BR-CERT-04, 06).
    /// </summary>
    private async Task<Learner> EnsureLearnerAsync(int courseId, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var course = (await courses.ListSummariesAsync(c => c.Id == courseId, ct)).FirstOrDefault() ?? throw new NotFoundException("Course not found.");
        var manager = access.Can(PermissionCodes.CoursesView, courseId);
        // An unpublished course looks like a missing one to people who may not open it (BR-CAT-02).
        if (!course.IsPublished && !manager) throw new NotFoundException("Course not found.");
        if (!access.Can(PermissionCodes.LearnView)) throw new ForbiddenException();
        var enrollment = await enrollments.GetForUserCourseAsync(access.UserId, courseId, ct);
        var enrolled = enrollment?.Status.Code == EnrollmentStatusCodes.Active;
        if (!enrolled && !manager) throw new ForbiddenException(NotEnrolledMessage);
        return new Learner(access, course, enrolled);
    }

    private async Task<CertificationStatusDto> BuildStatusAsync(Learner learner, CancellationToken ct)
    {
        var (userId, courseId) = (learner.Access.UserId, learner.Course.Id);
        var rule = await RuleOrDefaultAsync(courseId, ct);
        var percent = await ProgressPercentAsync(userId, courseId, ct);
        var handedIn = await submissions.ListForUserCourseAsync(userId, courseId, ct);
        var owned = await certificates.ListForUserCourseAsync(userId, courseId, ct);
        var valid = owned.FirstOrDefault(c => !c.IsRevoked);
        var revoked = valid is null ? owned.FirstOrDefault(c => c.IsRevoked) : null;

        var progressMet = percent >= rule.MinProgress;
        int? attemptsLeft = rule.MaxAttempts == 0 ? null : Math.Max(0, rule.MaxAttempts - handedIn.Count);
        var failedId = CertificationCodes.StatusId(CertificationCodes.Failed);
        var canSubmit = learner.IsEnrolled && rule.IsEnabled && CertificationCodes.ModeCode(rule.CertificationModeId) == CertificationCodes.Exam
            && progressMet && valid is null && revoked is null && handedIn.All(s => s.SubmissionStatusId == failedId) && attemptsLeft != 0;

        return new CertificationStatusDto(ToDto(rule), percent, progressMet, await ToDtosAsync(handedIn, ct), attemptsLeft, canSubmit,
            valid is null ? null : ToDto(valid), revoked is null ? null : ToDto(revoked));
    }

    /// <summary>Course % from the current curriculum, the same as the learning page shows (BR-LRN-15).</summary>
    private async Task<int> ProgressPercentAsync(int userId, int courseId, CancellationToken ct)
    {
        var sections = await courses.ListSectionsAsync([courseId], tracking: false, ct);
        var rows = await progress.ListForUserAsync(userId, [courseId], ct);
        return LessonRules.Build(courseId, sections, rows).Percent;
    }

    private async Task<CourseSummary> GetCourseAsync(int courseId, CancellationToken ct) =>
        (await courses.ListSummariesAsync(c => c.Id == courseId, ct)).FirstOrDefault() ?? throw new NotFoundException("Course not found.");

    private async Task<CertificationRule> RuleOrDefaultAsync(int courseId, CancellationToken ct) =>
        (await rules.ListAsync(r => r.CourseId == courseId, ct)).FirstOrDefault() ?? DefaultRule(courseId);

    /// <summary>The rule a course uses until someone saves one (BR-CERT-01).</summary>
    public static CertificationRule DefaultRule(int courseId) => new()
    {
        CourseId = courseId, IsEnabled = true, CertificationModeId = CertificationCodes.ModeId(CertificationCodes.Completion),
        MinProgress = 100, ExamTypeId = CertificationCodes.ExamTypeId(CertificationCodes.Project), ExamTitle = DefaultExamTitle,
        MaxScore = 100, PassingScore = 60, MaxAttempts = 0,
    };

    private static bool IsCompletionRule(CertificationRule rule) =>
        rule.IsEnabled && CertificationCodes.ModeCode(rule.CertificationModeId) == CertificationCodes.Completion;

    /// <summary>Courses whose certificates the user manages; null for all. 403 when there are none.</summary>
    private async Task<IReadOnlySet<int>?> ManagedCoursesAsync(CancellationToken ct)
    {
        var managed = (await currentUser.GetAccessAsync(ct)).CoursesWith(PermissionCodes.CertificatesManage);
        if (managed is { Count: 0 }) throw new ForbiddenException();
        return managed;
    }

    private async Task<Certificate> GetManagedCertificateAsync(int certificateId, CancellationToken ct)
    {
        var certificate = await certificates.GetByIdAsync(certificateId, ct) ?? throw new NotFoundException("Certificate not found.");
        await currentUser.EnsureCanAsync(PermissionCodes.CertificatesManage, certificate.CourseId, ct);
        return certificate;
    }

    /// <summary>Adds a certificate with snapshot names (BR-CERT-12). The caller saves.</summary>
    private async Task<Certificate> IssueAsync(int userId, CourseSummary course, string modeCode, int? examTypeId, int? scorePercent, CancellationToken ct)
    {
        var user = (await users.ListAsync(u => u.Id == userId, ct)).FirstOrDefault();
        var certificate = new Certificate
        {
            Code = await NewCodeAsync(ct), UserId = userId, CourseId = course.Id,
            UserName = user?.Name ?? "Student", CourseTitle = course.Title, InstructorName = course.InstructorName,
            CertificationModeId = CertificationCodes.ModeId(modeCode), ExamTypeId = examTypeId, ScorePercent = scorePercent,
            IssuedAt = clock.GetUtcNow().UtcDateTime,
        };
        await certificates.AddAsync(certificate, ct);
        return certificate;
    }

    /// <summary>
    /// Saves a new certificate. False when another request issued the user's certificate at the same moment: the
    /// unique index keeps one valid certificate per user and course (BR-CERT-03).
    /// </summary>
    private async Task<bool> TrySaveIssuedAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            return false;
        }
    }

    private async Task<CertificateDto?> ValidDtoAsync(int userId, int courseId, CancellationToken ct)
    {
        var valid = (await certificates.ListForUserCourseAsync(userId, courseId, ct)).FirstOrDefault(c => !c.IsRevoked);
        return valid is null ? null : ToDto(valid);
    }

    /// <summary>CA-YYYY-XXXXXX: issue year (UTC) and 6 characters without look-alikes (I, O, 0, 1). Unique (BR-CERT-14).</summary>
    private async Task<string> NewCodeAsync(CancellationToken ct)
    {
        string code;
        do
        {
            var suffix = new string(RandomNumberGenerator.GetBytes(6).Select(b => CodeAlphabet[b % CodeAlphabet.Length]).ToArray());
            code = $"CA-{clock.GetUtcNow().Year}-{suffix}";
        } while (await certificates.CodeExistsAsync(code, ct));
        return code;
    }

    private async Task<List<SubmissionDto>> ToDtosAsync(IReadOnlyCollection<ExamSubmission> list, CancellationToken ct)
    {
        if (list.Count == 0) return [];
        var userIds = list.Select(s => s.UserId).Concat(list.Where(s => s.ReviewedByUserId is not null).Select(s => s.ReviewedByUserId!.Value)).Distinct().ToList();
        var courseIds = list.Select(s => s.CourseId).Distinct().ToList();
        var people = (await users.ListAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id);
        var titles = (await courses.ListSummariesAsync(c => courseIds.Contains(c.Id), ct)).ToDictionary(c => c.Id, c => c.Title);
        var saved = await rules.ListForCoursesAsync(courseIds, ct);

        return list.Select(s =>
        {
            var rule = saved.GetValueOrDefault(s.CourseId) ?? DefaultRule(s.CourseId);
            var reviewer = s.ReviewedByUserId is { } id && people.TryGetValue(id, out var r) ? new UserSummaryDto(r.Id, r.Name, r.Email) : null;
            return new SubmissionDto(
                s.Id, s.UserId, people.TryGetValue(s.UserId, out var student) ? student.Name : "Former member",
                s.CourseId, titles.GetValueOrDefault(s.CourseId, ""), CertificationCodes.ExamTypeCode(s.ExamTypeId), rule.ExamTitle,
                rule.MaxScore, rule.PassingScore, s.Url, s.Notes ?? "", s.SubmittedAt, CertificationCodes.StatusCode(s.SubmissionStatusId),
                s.Score, s.Feedback, reviewer, s.ReviewedAt);
        }).ToList();
    }

    private static RuleDto ToDto(CertificationRule r) => new(
        r.CourseId, r.IsEnabled, CertificationCodes.ModeCode(r.CertificationModeId), r.MinProgress,
        CertificationCodes.ExamTypeCode(r.ExamTypeId), r.ExamTitle, r.ExamInstructions ?? "", r.MaxScore, r.PassingScore, r.MaxAttempts);

    public static CertificateDto ToDto(Certificate c) => new(
        c.Id, c.Code, c.UserId, c.CourseId, c.UserName, c.CourseTitle, c.InstructorName,
        CertificationCodes.ModeCode(c.CertificationModeId), c.ExamTypeId is { } type ? CertificationCodes.ExamTypeCode(type) : null,
        c.ScorePercent, c.IssuedAt, c.IsRevoked, c.RevokedReason);
}
