namespace CourseAcademy.Api.Entities;

/// <summary>How one course awards its certificate. At most one per course; without a row the default rule applies (BR-CERT-01).</summary>
public sealed class CertificationRule : BaseEntity
{
    public int CourseId { get; set; }
    public bool IsEnabled { get; set; } = true;
    /// <summary>completion: certified when the progress requirement is met. exam: also needs a passed central exam.</summary>
    public int CertificationModeId { get; set; }
    /// <summary>Share of the course's lessons that must be completed, 1–100.</summary>
    public int MinProgress { get; set; } = 100;
    public int ExamTypeId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string? ExamInstructions { get; set; }
    public int MaxScore { get; set; } = 100;
    public int PassingScore { get; set; } = 60;
    /// <summary>0 means unlimited.</summary>
    public int MaxAttempts { get; set; }
}

/// <summary>A student's central exam work: a link to the project or presentation, reviewed by an instructor.</summary>
public sealed class ExamSubmission : BaseEntity
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    /// <summary>The rule's exam type when the work was handed in.</summary>
    public int ExamTypeId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int SubmissionStatusId { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>
/// An issued certificate. Names are copied in, so later edits don't change it (BR-CERT-12). A user has at most one
/// valid (not revoked) certificate per course (BR-CERT-03).
/// </summary>
public sealed class Certificate : BaseEntity
{
    /// <summary>Public verification code, e.g. CA-2026-K7Q2XM.</summary>
    public string Code { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    /// <summary>Empty when the course had no instructor name.</summary>
    public string InstructorName { get; set; } = string.Empty;
    public int CertificationModeId { get; set; }
    /// <summary>Exam certificates only.</summary>
    public int? ExamTypeId { get; set; }
    /// <summary>Exam certificates only: score as a whole percentage.</summary>
    public int? ScorePercent { get; set; }
    public DateTime IssuedAt { get; set; }
    public bool IsRevoked { get; set; }
    /// <summary>Shown publicly on the verification page.</summary>
    public string? RevokedReason { get; set; }
}
