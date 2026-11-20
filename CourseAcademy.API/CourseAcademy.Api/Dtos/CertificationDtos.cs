namespace CourseAcademy.Api.Dtos;

// ── Rules ───────────────────────────────────────────────────────

/// <summary>A course's certification rule; the default rule when none was saved (BR-CERT-01).</summary>
public sealed record RuleDto(
    int CourseId, bool IsEnabled, string ModeCode, int MinProgress, string ExamTypeCode, string ExamTitle,
    string ExamInstructions, int MaxScore, int PassingScore, int MaxAttempts);

public sealed record RuleSaveRequest(
    bool IsEnabled, string ModeCode, int MinProgress, string ExamTypeCode, string? ExamTitle,
    string? ExamInstructions, int MaxScore, int PassingScore, int MaxAttempts);

// ── Certificates ────────────────────────────────────────────────

public sealed record CertificateDto(
    int Id, string Code, int UserId, int CourseId, string UserName, string CourseTitle, string InstructorName,
    string ModeCode, string? ExamTypeCode, int? ScorePercent, DateTime IssuedAt, bool IsRevoked, string? RevokedReason);

/// <summary>What the public verification page shows (BR-CERT-13).</summary>
public sealed record VerifyCertificateDto(
    string Code, bool IsRevoked, string? RevokedReason, string UserName, string CourseTitle, string ModeCode,
    string? ExamTypeCode, int? ScorePercent, string InstructorName, DateTime IssuedAt);

public sealed record RevokeCertificateRequest(string? Reason);

public sealed class CertificateListQuery : Common.PageQuery
{
    /// <summary>Matches the code, the student's name or the course title.</summary>
    public string? Search { get; set; }
    public int? CourseId { get; set; }
}

// ── Central exam ────────────────────────────────────────────────

/// <summary>A hand-in. ExamTitle, MaxScore and PassingScore come from the course's current rule.</summary>
public sealed record SubmissionDto(
    int Id, int UserId, string UserName, int CourseId, string CourseTitle, string ExamTypeCode, string ExamTitle,
    int MaxScore, int PassingScore, string Url, string Notes, DateTime SubmittedAt, string StatusCode,
    decimal? Score, string? Feedback, UserSummaryDto? ReviewedBy, DateTime? ReviewedAt);

public sealed record ExamSubmitRequest(string? Url, string? Notes);

public sealed record ReviewSubmissionRequest(decimal Score, string? Feedback);

/// <summary>Certificate is set when the score passed and a certificate was issued (BR-CERT-10).</summary>
public sealed record ReviewResultDto(SubmissionDto Submission, CertificateDto? Certificate);

public sealed class SubmissionListQuery : Common.PageQuery
{
    /// <summary>submitted (default), passed, failed or all.</summary>
    public string? Status { get; set; }
    public int? CourseId { get; set; }
}

// ── Student status ──────────────────────────────────────────────

/// <summary>
/// Where the signed-in user stands with one course's certificate. AttemptsLeft is null when attempts are unlimited.
/// RevokedCertificate is set when the user's certificate was revoked and none is valid (BR-CERT-17).
/// </summary>
public sealed record CertificationStatusDto(
    RuleDto Rule, int Progress, bool ProgressMet, IReadOnlyList<SubmissionDto> Submissions, int? AttemptsLeft,
    bool CanSubmit, CertificateDto? Certificate, CertificateDto? RevokedCertificate);
