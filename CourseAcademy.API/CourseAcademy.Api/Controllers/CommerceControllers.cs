using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

// Permission and ownership checks are made in the services.

/// <summary>Checkout and payments for the signed-in buyer, plus gateway callbacks.</summary>
[Authorize]
public sealed class PaymentsController(IPaymentService payments) : ApiControllerBase
{
    [HttpGet("checkout/{courseSlug}")]
    [ProducesResponseType<ApiResponse<CheckoutDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Checkout(string courseSlug, CancellationToken ct) => Success(await payments.GetCheckoutAsync(courseSlug, ct));

    /// <summary>Starts a payment through the active gateway. The amount comes from the course, not from the request.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<StartPaymentDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Start(StartPaymentRequest request, CancellationToken ct) => Created(await payments.StartAsync(request, ct));

    [HttpGet("mine")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<TransactionDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken ct) => Success(await payments.GetMineAsync(ct));

    /// <summary>A payment's current status (receipt). Visible to the buyer and to people who manage payments.</summary>
    [HttpGet("{transactionNo}")]
    [ProducesResponseType<ApiResponse<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string transactionNo, CancellationToken ct) => Success(await payments.GetAsync(transactionNo, ct));

    /// <summary>Development only: the outcome picked on the simulated payment page.</summary>
    [HttpPost("{transactionNo}/demo-outcome")]
    [ProducesResponseType<ApiResponse<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DemoOutcome(string transactionNo, DemoOutcomeRequest request, CancellationToken ct) =>
        Success(await payments.CompleteDemoPaymentAsync(transactionNo, request, ct));

    /// <summary>
    /// Server-to-server callback (IPN) from a payment provider. Anonymous: the gateway verifies the call itself
    /// (signature and a status check with the provider) before anything changes.
    /// </summary>
    [HttpPost("callback/{gateway}")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Callback(string gateway, CancellationToken ct)
    {
        // Gateways sign the raw body, and some also post a form: keep the body readable twice.
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;
        var form = Request.HasFormContentType ? (await Request.ReadFormAsync(ct)).ToDictionary(f => f.Key, f => f.Value.ToString()) : [];
        var callback = new GatewayCallback(
            body,
            Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase),
            Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
            form);
        return await payments.HandleCallbackAsync(gateway, callback, ct)
            ? Success()
            : BadRequest(ApiResponse.Fail("The callback could not be verified."));
    }
}

[Authorize]
public sealed class EnrollmentsController(IEnrollmentService enrollments) : ApiControllerBase
{
    [HttpGet("mine")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<EnrollmentDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken ct) => Success(await enrollments.GetMineAsync(ct));

    /// <summary>Enrolls the signed-in user in a free course.</summary>
    [HttpPost("free/{courseSlug}")]
    [ProducesResponseType<ApiResponse<EnrollmentDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> EnrollFree(string courseSlug, CancellationToken ct) => Created(await enrollments.EnrollFreeAsync(courseSlug, ct));
}

[Authorize]
[Route("api/v1/admin/transactions")]
public sealed class AdminTransactionsController(IPaymentService payments) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<TransactionPageDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] TransactionListQuery query, CancellationToken ct) => Success(await payments.GetAllAsync(query, ct));

    [HttpPatch("{transactionNo}/status")]
    [ProducesResponseType<ApiResponse<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetStatus(string transactionNo, SetTransactionStatusRequest request, CancellationToken ct) =>
        Success(await payments.SetStatusAsync(transactionNo, request.StatusCode, ct));

    [HttpDelete("{transactionNo}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string transactionNo, CancellationToken ct)
    {
        await payments.DeleteAsync(transactionNo, ct);
        return Success();
    }
}

[Authorize]
[Route("api/v1/admin/enrollments")]
public sealed class AdminEnrollmentsController(IEnrollmentService enrollments) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<EnrollmentDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] EnrollmentListQuery query, CancellationToken ct) => Success(await enrollments.GetAllAsync(query, ct));

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<ApiResponse<EnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetStatus(int id, SetEnrollmentStatusRequest request, CancellationToken ct) =>
        Success(await enrollments.SetStatusAsync(id, request.StatusCode, ct));
}
