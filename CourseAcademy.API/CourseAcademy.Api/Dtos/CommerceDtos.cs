namespace CourseAcademy.Api.Dtos;

// ── Checkout ────────────────────────────────────────────────────

public sealed record CheckoutDto(
    CourseCardDto Course, decimal OriginalPrice, decimal DiscountAmount, decimal Amount, int DiscountPercent,
    bool IsFree, bool AlreadyEnrolled, IReadOnlyList<PaymentMethodDto> Methods);

public sealed record StartPaymentRequest(string CourseSlug, string MethodCode);

/// <summary>IsExternal: true when RedirectUrl is the provider's hosted page; false for a page inside the frontend (demo).</summary>
public sealed record StartPaymentDto(string TransactionNo, string RedirectUrl, bool IsExternal);

/// <summary>Outcome picked on the demo payment page: "success", "failed" or "cancelled".</summary>
public sealed record DemoOutcomeRequest(string Outcome);

// ── Transactions ────────────────────────────────────────────────

public sealed record TransactionDto(
    string TransactionNo, string Gateway, int UserId, string UserName, string UserEmail,
    int CourseId, string CourseSlug, string CourseTitle, string MethodCode, string? MaskedAccount,
    decimal OriginalAmount, decimal DiscountAmount, decimal Amount, string Currency, string StatusCode,
    string? FailureReason, DateTime? FulfilledAt, DateTime ExpiresAt, DateTime CreatedAt, DateTime? UpdatedAt,
    IReadOnlyList<string> AllowedActions, bool CanOpenCourse);

public sealed class TransactionListQuery : Common.PageQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Method { get; set; }
    public int? CourseId { get; set; }
    /// <summary>Inclusive Bangladesh day "YYYY-MM-DD".</summary>
    public string? From { get; set; }
    /// <summary>Inclusive Bangladesh day "YYYY-MM-DD".</summary>
    public string? To { get; set; }
}

public sealed record TransactionTotalsDto(decimal Collected, decimal Refunded, int Successful, int Unsuccessful);

public sealed record CourseOptionDto(int Id, string Title);

/// <summary>A page of transactions; Totals cover every transaction matching the filters, not only this page.</summary>
public sealed record TransactionPageDto(
    IReadOnlyList<TransactionDto> Items, int Total, int Page, int PageSize, TransactionTotalsDto Totals, IReadOnlyList<CourseOptionDto> CourseOptions);

public sealed record SetTransactionStatusRequest(string StatusCode);

// ── Enrollments ─────────────────────────────────────────────────

public sealed record EnrollmentDto(
    int Id, int UserId, string StudentName, string StudentEmail, int CourseId, string CourseTitle,
    string StatusCode, DateTime EnrolledAt, string? TransactionNo);

public sealed class EnrollmentListQuery : Common.PageQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public sealed record SetEnrollmentStatusRequest(string StatusCode);
