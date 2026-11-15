namespace CourseAcademy.Api.Entities;

/// <summary>A user's access to a course. One row per user and course; buying again re-activates it (BR-ENR-01).</summary>
public sealed class Enrollment : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int EnrollmentStatusId { get; set; }
    public EnrollmentStatus Status { get; set; } = null!;
    public DateTime EnrolledAt { get; set; }

    /// <summary>The payment that granted access. Null for free courses and admin restores.</summary>
    public int? PaymentTransactionId { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
}

/// <summary>
/// One payment attempt. User and course details are copied in, so the record outlives later edits or deletion.
/// The gateway that handled it is stored by name, so payments from different gateways can coexist.
/// </summary>
public sealed class PaymentTransaction : BaseEntity
{
    /// <summary>Public id shown to users and used in URLs, e.g. DEMO-20261115-K7Q2XM. The prefix comes from the gateway.</summary>
    public string TransactionNo { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    /// <summary>The gateway's own id for this payment, when it has one.</summary>
    public string? GatewayReference { get; set; }

    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public string CourseSlug { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;

    public int PaymentMethodId { get; set; }
    public PaymentMethod Method { get; set; } = null!;
    /// <summary>Masked card or wallet number. Full details are never stored.</summary>
    public string? MaskedAccount { get; set; }

    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary>Charged amount, taken from the course price on the server (BR-PAY-05).</summary>
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public int PaymentStatusId { get; set; }
    public PaymentStatus Status { get; set; } = null!;
    public string? FailureReason { get; set; }

    /// <summary>A pending payment not finished by then is cancelled.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set once course access has been granted, so it is granted only once (BR-PAY-08).</summary>
    public DateTime? FulfilledAt { get; set; }
}
