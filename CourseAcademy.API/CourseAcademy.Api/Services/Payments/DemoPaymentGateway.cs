namespace CourseAcademy.Api.Services.Payments;

/// <summary>
/// Stands in for a real provider during development. No money moves: the buyer picks the outcome on the
/// frontend's simulated payment page, which reports it through <see cref="IPaymentService.CompleteDemoPaymentAsync"/>.
/// Never allowed in production.
/// </summary>
public sealed class DemoPaymentGateway : IPaymentGateway
{
    public const string GatewayName = "demo";

    /// <summary>Fixed test details per method, the same the frontend shows. Never real credentials.</summary>
    private static readonly Dictionary<string, (string Masked, string FailureReason)> Methods = new()
    {
        ["bkash"] = ("017•••••000", "Insufficient wallet balance (simulated)"),
        ["nagad"] = ("018•••••000", "Insufficient wallet balance (simulated)"),
        ["rocket"] = ("019••••••00-0", "Transaction limit exceeded (simulated)"),
        ["card"] = ("•••• •••• •••• 4242", "Card declined by issuer (simulated)"),
    };

    public string Name => GatewayName;
    public string TransactionPrefix => "DEMO";
    public bool AllowedInProduction => false;

    public Task<GatewaySession> InitiateAsync(GatewayPaymentRequest request, CancellationToken ct) =>
        Task.FromResult(new GatewaySession($"/checkout/pay/{request.TransactionNo}", null, Methods.GetValueOrDefault(request.MethodCode).Masked));

    /// <summary>The demo has no provider sending callbacks.</summary>
    public Task<GatewayResult?> VerifyCallbackAsync(GatewayCallback callback, CancellationToken ct) => Task.FromResult<GatewayResult?>(null);

    /// <summary>The demo provider knows nothing beyond what is already stored.</summary>
    public Task<GatewayResult?> QueryStatusAsync(string transactionNo, string? gatewayReference, CancellationToken ct) =>
        Task.FromResult<GatewayResult?>(null);

    public static string FailureReasonFor(string methodCode) =>
        Methods.TryGetValue(methodCode, out var method) ? method.FailureReason : "Payment declined (simulated)";
}
