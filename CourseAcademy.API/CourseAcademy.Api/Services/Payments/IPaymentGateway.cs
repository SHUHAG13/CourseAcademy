namespace CourseAcademy.Api.Services.Payments;

/// <summary>What the gateway needs to start a payment. The amount was read from the database, never from the browser.</summary>
public sealed record GatewayPaymentRequest(
    string TransactionNo, decimal Amount, string Currency, string MethodCode,
    string CourseTitle, string CustomerName, string CustomerEmail, DateTime ExpiresAt);

/// <summary>
/// Where to send the buyer next. RedirectUrl is an absolute URL of the gateway's hosted page, or for the demo
/// gateway a path inside the frontend. MaskedAccount is filled when the gateway knows it already.
/// </summary>
public sealed record GatewaySession(string RedirectUrl, string? GatewayReference, string? MaskedAccount);

public enum GatewayOutcome
{
    Pending,
    Success,
    Failed,
    Cancelled,
}

/// <summary>A payment result the gateway has confirmed (signed callback or status query).</summary>
public sealed record GatewayResult(
    string TransactionNo, GatewayOutcome Outcome, string? FailureReason = null, string? GatewayReference = null, string? MaskedAccount = null);

/// <summary>A callback as received over HTTP, without ASP.NET types, so gateways stay easy to test.</summary>
public sealed record GatewayCallback(
    string RawBody, IReadOnlyDictionary<string, string> Headers, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Form);

/// <summary>
/// One payment provider (demo, SSLCOMMERZ, bKash, aamarPay…). It only talks to the provider:
/// starting a payment, checking a callback is genuine, and asking for a payment's status.
/// All business rules (status changes, granting access once, refunds) live in <see cref="IPaymentService"/>.
/// To add a provider: implement this interface, register it in DI, and set Payments:Gateway to its <see cref="Name"/>.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Short, lower-case, stable name, stored with each payment and used in the callback URL.</summary>
    string Name { get; }

    /// <summary>Start of every transaction number this gateway creates, e.g. "DEMO".</summary>
    string TransactionPrefix { get; }

    /// <summary>False for gateways that must never take part in production (the demo gateway).</summary>
    bool AllowedInProduction { get; }

    Task<GatewaySession> InitiateAsync(GatewayPaymentRequest request, CancellationToken ct);

    /// <summary>
    /// Checks a callback really comes from the provider (signature, then a server-to-server status check)
    /// and returns its result. Returns null when the callback is not genuine or not understood.
    /// </summary>
    Task<GatewayResult?> VerifyCallbackAsync(GatewayCallback callback, CancellationToken ct);

    /// <summary>Asks the provider for the current status of a payment, or null if it cannot tell.</summary>
    Task<GatewayResult?> QueryStatusAsync(string transactionNo, string? gatewayReference, CancellationToken ct);
}

/// <summary>Finds gateways by name and knows which one new payments use (Payments:Gateway).</summary>
public interface IPaymentGatewayResolver
{
    IPaymentGateway Active { get; }
    IPaymentGateway? Find(string name);
}

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly Dictionary<string, IPaymentGateway> _gateways;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways, Microsoft.Extensions.Options.IOptions<PaymentSettings> settings)
    {
        _gateways = gateways.ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);
        Active = Find(settings.Value.Gateway)
            ?? throw new InvalidOperationException($"Payments:Gateway \"{settings.Value.Gateway}\" is not a registered payment gateway.");
    }

    public IPaymentGateway Active { get; }

    public IPaymentGateway? Find(string name) => _gateways.GetValueOrDefault(name);
}

/// <summary>Section "Payments" in configuration.</summary>
public sealed class PaymentSettings
{
    public const string SectionName = "Payments";

    /// <summary>Name of the gateway new payments use.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string Gateway { get; set; } = string.Empty;

    /// <summary>Minutes a payment may stay pending before it is cancelled.</summary>
    [System.ComponentModel.DataAnnotations.Range(5, 24 * 60)]
    public int PendingMinutes { get; set; } = 30;
}
