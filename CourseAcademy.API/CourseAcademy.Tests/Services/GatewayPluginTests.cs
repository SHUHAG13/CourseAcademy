using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Tests.Services;

/// <summary>
/// A made-up provider shows that a new gateway only implements <see cref="IPaymentGateway"/>:
/// PaymentService and the payment rules stay the same.
/// </summary>
public class GatewayPluginTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    /// <summary>Hosted page on another site; callbacks are "signed" with a shared secret header.</summary>
    private sealed class FakeProviderGateway : IPaymentGateway
    {
        public const string Secret = "s3cret";
        public string Name => "fakepay";
        public string TransactionPrefix => "FP";
        public bool AllowedInProduction => true;

        public Task<GatewaySession> InitiateAsync(GatewayPaymentRequest request, CancellationToken ct) =>
            Task.FromResult(new GatewaySession($"https://pay.example.com/session/{request.TransactionNo}?amount={request.Amount}", "fp-ref-1", null));

        public Task<GatewayResult?> VerifyCallbackAsync(GatewayCallback callback, CancellationToken ct) =>
            Task.FromResult(callback.Headers.GetValueOrDefault("X-Signature") == Secret
                ? new GatewayResult(callback.Form["tran_id"], callback.Form["status"] == "VALID" ? GatewayOutcome.Success : GatewayOutcome.Failed,
                    "Declined by bank", "fp-ref-1", "01•••••99")
                : null);

        public Task<GatewayResult?> QueryStatusAsync(string transactionNo, string? gatewayReference, CancellationToken ct) => Task.FromResult<GatewayResult?>(null);
    }

    private static PaymentService ServiceWith(CatalogTests.Kit kit, IPaymentGateway gateway)
    {
        var db = kit.Identity.Db;
        var settings = Options.Create(new PaymentSettings { Gateway = gateway.Name, PendingMinutes = 30 });
        var enrollmentRepository = new EnrollmentRepository(db);
        return new PaymentService(new PaymentRepository(db), new CourseRepository(db), enrollmentRepository,
            new EnrollmentService(enrollmentRepository, new CourseRepository(db), new UnitOfWork(db), kit.Identity.CurrentUser, kit.Identity.Clock),
            kit.Identity.Users, new LookupRepository(db), new PaymentGatewayResolver([new DemoPaymentGateway(), gateway], settings),
            new UnitOfWork(db), kit.Identity.CurrentUser, settings, kit.Identity.Clock, NullLogger<PaymentService>.Instance);
    }

    private static GatewayCallback Callback(string transactionNo, string status, string signature) => new(
        string.Empty,
        new Dictionary<string, string> { ["X-Signature"] = signature },
        new Dictionary<string, string>(),
        new Dictionary<string, string> { ["tran_id"] = transactionNo, ["status"] = status });

    [Fact]
    public async Task A_new_provider_plugs_in_with_its_own_prefix_page_and_verified_callbacks()
    {
        var kit = new CatalogTests.Kit().As(IdentityTestKit.Admin);
        var course = await kit.Admin.CreateAsync(kit.Course("plug-course") with { OriginalPrice = 5000, Price = 4000 }, Ct);
        await kit.Admin.SaveCurriculumAsync(course.Id, CatalogTests.TwoSections(), Ct);
        await kit.Admin.SetPublishedAsync(course.Id, true, Ct);
        kit.As(IdentityTestKit.Student);
        var payments = ServiceWith(kit, new FakeProviderGateway());

        var started = await payments.StartAsync(new StartPaymentRequest("plug-course", "bkash"), Ct);
        Assert.StartsWith("FP-", started.TransactionNo);
        Assert.True(started.IsExternal);
        Assert.Contains("amount=4000", started.RedirectUrl);

        // A forged callback changes nothing.
        Assert.False(await payments.HandleCallbackAsync("fakepay", Callback(started.TransactionNo, "VALID", "wrong"), Ct));
        Assert.Equal(PaymentStatusCodes.Pending, (await payments.GetAsync(started.TransactionNo, Ct)).StatusCode);

        // The provider confirms after the payment window: the buyer has paid, so it still counts.
        kit.Identity.Clock.Advance(TimeSpan.FromMinutes(40));
        await payments.ExpireStaleAsync(Ct);
        Assert.True(await payments.HandleCallbackAsync("fakepay", Callback(started.TransactionNo, "VALID", FakeProviderGateway.Secret), Ct));

        var done = await payments.GetAsync(started.TransactionNo, Ct);
        Assert.Equal((PaymentStatusCodes.Success, "fakepay", "01•••••99"), (done.StatusCode, done.Gateway, done.MaskedAccount));
        Assert.True(done.CanOpenCourse);

        // The demo page cannot finish a payment that belongs to another gateway.
        await Assert.ThrowsAsync<CourseAcademy.Api.Common.Exceptions.ConflictException>(() =>
            payments.CompleteDemoPaymentAsync(started.TransactionNo, new DemoOutcomeRequest("success"), Ct));
    }
}
