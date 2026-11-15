using System.Security.Cryptography;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Services.Payments;

public static class PaymentStatusCodes
{
    public const string Pending = "pending";
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";
}

/// <summary>
/// Checkout and payment records. Every business rule about payments lives here, the same for every gateway:
/// the server sets the amount, a pending payment is reused or replaced, access is granted once per payment,
/// admins may only make the allowed status changes, and a refund removes access.
/// </summary>
public interface IPaymentService
{
    Task<CheckoutDto> GetCheckoutAsync(string courseSlug, CancellationToken ct);
    Task<StartPaymentDto> StartAsync(StartPaymentRequest request, CancellationToken ct);
    Task<TransactionDto> GetAsync(string transactionNo, CancellationToken ct);
    Task<IReadOnlyList<TransactionDto>> GetMineAsync(CancellationToken ct);

    /// <summary>Records the outcome the buyer picked on the demo payment page. Only for payments made through the demo gateway.</summary>
    Task<TransactionDto> CompleteDemoPaymentAsync(string transactionNo, DemoOutcomeRequest request, CancellationToken ct);

    /// <summary>Handles a provider's callback. Returns false when the gateway could not verify it.</summary>
    Task<bool> HandleCallbackAsync(string gatewayName, GatewayCallback callback, CancellationToken ct);

    Task<TransactionPageDto> GetAllAsync(TransactionListQuery query, CancellationToken ct);
    Task<TransactionDto> SetStatusAsync(string transactionNo, string statusCode, CancellationToken ct);
    Task DeleteAsync(string transactionNo, CancellationToken ct);

    /// <summary>Cancels pending payments whose time ran out. Returns how many were cancelled.</summary>
    Task<int> ExpireStaleAsync(CancellationToken ct);
}

public sealed class PaymentService(
    IPaymentRepository payments,
    ICourseRepository courses,
    IEnrollmentRepository enrollmentRepository,
    IEnrollmentService enrollments,
    IUserRepository users,
    ILookupRepository lookups,
    IPaymentGatewayResolver gateways,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<PaymentSettings> settings,
    TimeProvider clock,
    ILogger<PaymentService> logger) : IPaymentService
{
    public const string ExpiredReason = "The payment time ran out.";
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Status changes an admin may make; anything else is refused (BR-PAY-10).</summary>
    private static readonly Dictionary<string, string[]> AdminTransitions = new()
    {
        [PaymentStatusCodes.Pending] = [PaymentStatusCodes.Success, PaymentStatusCodes.Cancelled],
        [PaymentStatusCodes.Success] = [PaymentStatusCodes.Refunded],
        [PaymentStatusCodes.Failed] = [],
        [PaymentStatusCodes.Cancelled] = [],
        [PaymentStatusCodes.Refunded] = [],
    };

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // ── Buyer ───────────────────────────────────────────────────

    public async Task<CheckoutDto> GetCheckoutAsync(string courseSlug, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CheckoutView, null, ct);
        var course = await PublishedCourseAsync(courseSlug, ct);
        var enrolled = (await enrollmentRepository.ActiveCourseIdsAsync(currentUser.RequireUserId(), ct)).Contains(course.Id);
        var sectionCount = (await courses.CountSectionsAsync([course.Id], ct)).GetValueOrDefault(course.Id);
        var methods = (await lookups.ListAsync<PaymentMethod>(ct))
            .Select(m => new PaymentMethodDto(m.Code, m.Name, m.Description, m.LogoUrl, m.Icon)).ToList();
        return new CheckoutDto(
            CourseMapper.ToCard(course, sectionCount), course.OriginalPrice, course.OriginalPrice - course.Price, course.Price,
            CourseMapper.DiscountPercent(course.Price, course.OriginalPrice), course.Price == 0, enrolled, methods);
    }

    public async Task<StartPaymentDto> StartAsync(StartPaymentRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CheckoutView, null, ct);
        var userId = currentUser.RequireUserId();
        var course = await PublishedCourseAsync(request.CourseSlug, ct);
        if (course.Price == 0) throw new ConflictException("This course is free. Enroll without paying.");
        if ((await enrollmentRepository.ActiveCourseIdsAsync(userId, ct)).Contains(course.Id))
        {
            throw new ConflictException("You're already enrolled in this course.");
        }
        var method = await lookups.FindByCodeAsync<PaymentMethod>(request.MethodCode, ct)
            ?? throw new BusinessRuleException("Choose a payment method.", "methodCode");
        var gateway = gateways.Active;

        // One open payment per course: the same method continues it, another method replaces it (BR-PAY-19).
        foreach (var pending in await payments.ListPendingAsync(userId, course.Id, ct))
        {
            if (pending.ExpiresAt <= Now)
            {
                Expire(pending);
            }
            else if (pending.PaymentMethodId == method.Id && pending.Gateway == gateway.Name && pending.Amount == course.Price)
            {
                await unitOfWork.SaveChangesAsync(ct);
                var again = await gateway.InitiateAsync(ToGatewayRequest(pending, method.Code), ct);
                return new StartPaymentDto(pending.TransactionNo, again.RedirectUrl, IsExternal(again.RedirectUrl));
            }
            else
            {
                SetStatus(pending, PaymentStatusCodes.Cancelled, "Replaced by a new payment.");
            }
        }

        var user = await users.GetByIdAsync(userId, ct) ?? throw new UnauthorizedException();
        var transaction = new PaymentTransaction
        {
            TransactionNo = await NewTransactionNoAsync(gateway.TransactionPrefix, ct),
            Gateway = gateway.Name,
            UserId = user.Id,
            UserName = user.Name,
            UserEmail = user.Email,
            CourseId = course.Id,
            CourseSlug = course.Slug,
            CourseTitle = course.Title,
            PaymentMethodId = method.Id,
            OriginalAmount = course.OriginalPrice,
            DiscountAmount = course.OriginalPrice - course.Price,
            Amount = course.Price,
            CurrencyId = LookupIds.Currency("BDT"),
            PaymentStatusId = LookupIds.PaymentStatus(PaymentStatusCodes.Pending),
            ExpiresAt = Now.AddMinutes(settings.Value.PendingMinutes),
        };
        await payments.AddAsync(transaction, ct);
        await unitOfWork.SaveChangesAsync(ct);

        GatewaySession session;
        try
        {
            session = await gateway.InitiateAsync(ToGatewayRequest(transaction, method.Code), ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Gateway {Gateway} could not start payment {TransactionNo}.", gateway.Name, transaction.TransactionNo);
            SetStatus(transaction, PaymentStatusCodes.Failed, "The payment provider could not be reached.");
            await unitOfWork.SaveChangesAsync(ct);
            throw new ConflictException("We could not start the payment. Please try again in a moment.");
        }

        transaction.GatewayReference = session.GatewayReference;
        transaction.MaskedAccount = session.MaskedAccount;
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Payment {TransactionNo} started for course {CourseId} through {Gateway}.", transaction.TransactionNo, course.Id, gateway.Name);
        return new StartPaymentDto(transaction.TransactionNo, session.RedirectUrl, IsExternal(session.RedirectUrl));
    }

    public async Task<TransactionDto> GetAsync(string transactionNo, CancellationToken ct)
    {
        var transaction = await VisibleAsync(transactionNo, ct);
        if (StatusOf(transaction) == PaymentStatusCodes.Pending)
        {
            if (transaction.ExpiresAt <= Now)
            {
                Expire(transaction);
                await SaveIgnoringRaceAsync(ct);
            }
            else if (await gateways.Find(transaction.Gateway)!.QueryStatusAsync(transaction.TransactionNo, transaction.GatewayReference, ct) is { } result)
            {
                // The provider may know the result before its callback arrives.
                await ApplyResultAsync(transaction, result, ct);
            }
        }
        return await ToDtoAsync(transaction, admin: false, ct);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetMineAsync(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await payments.ListQuery().Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        var access = await enrollmentRepository.ActiveCourseIdsAsync(userId, ct);
        return list.Select(t => ToDto(t, admin: false, access.Contains(t.CourseId))).ToList();
    }

    public async Task<TransactionDto> CompleteDemoPaymentAsync(string transactionNo, DemoOutcomeRequest request, CancellationToken ct)
    {
        var transaction = await payments.GetByNoAsync(transactionNo, ct);
        // Only the buyer can finish their own payment on the demo page (BR-PAY-09).
        if (transaction is null || transaction.UserId != currentUser.RequireUserId()) throw new NotFoundException("Payment not found.");
        if (transaction.Gateway != DemoPaymentGateway.GatewayName || gateways.Find(DemoPaymentGateway.GatewayName) is null)
        {
            throw new ConflictException("This payment was not made through the demo gateway.");
        }
        var outcome = request.Outcome switch
        {
            PaymentStatusCodes.Success => GatewayOutcome.Success,
            PaymentStatusCodes.Failed => GatewayOutcome.Failed,
            PaymentStatusCodes.Cancelled => GatewayOutcome.Cancelled,
            _ => throw new BusinessRuleException("Choose success, failed or cancelled.", "outcome"),
        };

        if (StatusOf(transaction) != PaymentStatusCodes.Pending)
        {
            // Already finished. Unlike a real provider, the demo page moves no money, so a late "success" changes nothing.
        }
        else if (transaction.ExpiresAt <= Now)
        {
            Expire(transaction);
            await SaveIgnoringRaceAsync(ct);
        }
        else
        {
            var reason = outcome == GatewayOutcome.Failed ? DemoPaymentGateway.FailureReasonFor(transaction.Method.Code) : null;
            await ApplyResultAsync(transaction, new GatewayResult(transaction.TransactionNo, outcome, reason), ct);
        }
        return await ToDtoAsync(transaction, admin: false, ct);
    }

    public async Task<bool> HandleCallbackAsync(string gatewayName, GatewayCallback callback, CancellationToken ct)
    {
        var gateway = gateways.Find(gatewayName);
        if (gateway is null) return false;
        var result = await gateway.VerifyCallbackAsync(callback, ct);
        if (result is null)
        {
            logger.LogWarning("Gateway {Gateway} rejected a callback.", gatewayName);
            return false;
        }
        var transaction = await payments.GetByNoAsync(result.TransactionNo, ct);
        if (transaction is null || transaction.Gateway != gateway.Name)
        {
            logger.LogWarning("Callback from {Gateway} for unknown payment {TransactionNo}.", gatewayName, result.TransactionNo);
            return false;
        }
        await ApplyResultAsync(transaction, result, ct);
        return true;
    }

    // ── Admin ───────────────────────────────────────────────────

    public async Task<TransactionPageDto> GetAllAsync(TransactionListQuery q, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.TransactionsView, null, ct);
        var all = payments.ListQuery();
        var query = all;
        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status != "all") query = query.Where(t => t.Status.Code == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Method) && q.Method != "all") query = query.Where(t => t.Method.Code == q.Method);
        if (q.CourseId is { } courseId) query = query.Where(t => t.CourseId == courseId);
        // The days are the admin's Bangladesh days; stored times are UTC (BR-PAY-13).
        if (BangladeshTime.TryParseToUtc($"{q.From}T00:00", out var from)) query = query.Where(t => t.CreatedAt >= from);
        if (BangladeshTime.TryParseToUtc($"{q.To}T00:00", out var to))
        {
            var end = to.AddDays(1);
            query = query.Where(t => t.CreatedAt < end);
        }
        var term = q.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(t => t.TransactionNo.Contains(term) || t.UserName.Contains(term) || t.UserEmail.Contains(term) || t.CourseTitle.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var totals = await query.GroupBy(t => t.Status.Code).Select(g => new { g.Key, Sum = g.Sum(t => t.Amount), Count = g.Count() }).ToListAsync(ct);
        var page = await query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Skip(q.Skip).Take(q.PageSize).ToListAsync(ct);
        var options = await all.GroupBy(t => t.CourseId).Select(g => new CourseOptionDto(g.Key, g.OrderByDescending(t => t.CreatedAt).First().CourseTitle)).ToListAsync(ct);

        decimal Sum(string code) => totals.Where(x => x.Key == code).Sum(x => x.Sum);
        int Count(params string[] codes) => totals.Where(x => codes.Contains(x.Key)).Sum(x => x.Count);
        return new TransactionPageDto(
            page.Select(t => ToDto(t, admin: true, canOpenCourse: false)).ToList(), total, q.Page, q.PageSize,
            new TransactionTotalsDto(Sum(PaymentStatusCodes.Success), Sum(PaymentStatusCodes.Refunded), Count(PaymentStatusCodes.Success),
                Count(PaymentStatusCodes.Failed, PaymentStatusCodes.Cancelled)),
            options.OrderBy(o => o.Title).ToList());
    }

    public async Task<TransactionDto> SetStatusAsync(string transactionNo, string statusCode, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.TransactionsEdit, null, ct);
        var transaction = await payments.GetByNoAsync(transactionNo, ct) ?? throw new NotFoundException("Transaction not found.");
        if (!AdminTransitions[StatusOf(transaction)].Contains(statusCode))
        {
            throw new ConflictException("This change is not allowed for this transaction.");
        }

        SetStatus(transaction, statusCode, transaction.FailureReason);
        if (statusCode == PaymentStatusCodes.Success) await FulfilAsync(transaction, ct);
        if (statusCode == PaymentStatusCodes.Refunded) await enrollments.RevokeForPaymentAsync(transaction.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Payment {TransactionNo} set to {Status} by user {UserId}.", transactionNo, statusCode, currentUser.UserId);
        return await ToDtoAsync(transaction, admin: true, ct);
    }

    public async Task DeleteAsync(string transactionNo, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.TransactionsDelete, null, ct);
        var transaction = await payments.GetByNoAsync(transactionNo, ct) ?? throw new NotFoundException("Transaction not found.");
        // A successful payment is kept as a record; refund it first (BR-PAY-12).
        if (StatusOf(transaction) == PaymentStatusCodes.Success)
        {
            throw new ConflictException("Refund a successful payment before deleting it.");
        }
        payments.Remove(transaction);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<int> ExpireStaleAsync(CancellationToken ct)
    {
        var expired = await payments.ListExpiredAsync(Now, take: 200, ct);
        foreach (var transaction in expired) Expire(transaction);
        if (expired.Count > 0) await SaveIgnoringRaceAsync(ct);
        return expired.Count;
    }

    // ── Rules shared by every gateway ───────────────────────────

    /// <summary>
    /// Applies a confirmed result. Only a pending payment changes, so repeated callbacks are harmless.
    /// A success that arrives after the payment timed out is still accepted: the buyer has paid (BR-PAY-21).
    /// </summary>
    private async Task ApplyResultAsync(PaymentTransaction transaction, GatewayResult result, CancellationToken ct)
    {
        var status = StatusOf(transaction);
        var lateSuccess = status == PaymentStatusCodes.Cancelled && transaction.FailureReason == ExpiredReason && result.Outcome == GatewayOutcome.Success;
        if ((status != PaymentStatusCodes.Pending && !lateSuccess) || result.Outcome == GatewayOutcome.Pending) return;

        transaction.GatewayReference ??= result.GatewayReference;
        transaction.MaskedAccount = result.MaskedAccount ?? transaction.MaskedAccount;
        switch (result.Outcome)
        {
            case GatewayOutcome.Success:
                SetStatus(transaction, PaymentStatusCodes.Success, null);
                await FulfilAsync(transaction, ct);
                break;
            case GatewayOutcome.Failed:
                SetStatus(transaction, PaymentStatusCodes.Failed, result.FailureReason ?? "The payment was declined.");
                break;
            case GatewayOutcome.Cancelled:
                SetStatus(transaction, PaymentStatusCodes.Cancelled, null);
                break;
        }
        await SaveIgnoringRaceAsync(ct);
    }

    /// <summary>Grants course access once per payment (BR-PAY-08). A deleted course is not re-opened.</summary>
    private async Task FulfilAsync(PaymentTransaction transaction, CancellationToken ct)
    {
        if (transaction.FulfilledAt is not null) return;
        transaction.FulfilledAt = Now;
        if (await courses.GetByIdAsync(transaction.CourseId, ct) is not null)
        {
            await enrollments.GrantAsync(transaction.UserId, transaction.CourseId, transaction, Now, ct);
        }
    }

    private static void Expire(PaymentTransaction transaction) => SetStatus(transaction, PaymentStatusCodes.Cancelled, ExpiredReason);

    private static void SetStatus(PaymentTransaction transaction, string statusCode, string? failureReason)
    {
        // Only the foreign key changes; the code is read back from the fixed lookup ids (StatusOf).
        transaction.PaymentStatusId = LookupIds.PaymentStatus(statusCode);
        transaction.FailureReason = failureReason;
    }

    private static string StatusOf(PaymentTransaction transaction) =>
        Data.Seed.LookupSeed.PaymentStatuses.Single(s => s.Id == transaction.PaymentStatusId).Code;

    /// <summary>
    /// Two requests may finish the same payment at once (callback and receipt page). The status column is a
    /// concurrency token, so only the first save wins; the second request just reads the result.
    /// </summary>
    private async Task SaveIgnoringRaceAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Another request finished the same payment first.");
        }
    }

    private async Task<PaymentTransaction> VisibleAsync(string transactionNo, CancellationToken ct)
    {
        var transaction = await payments.GetByNoAsync(transactionNo, ct);
        var access = await currentUser.GetAccessAsync(ct);
        // The buyer and people who manage payments may see a payment; others are told it does not exist.
        if (transaction is null || (transaction.UserId != access.UserId && !access.Can(PermissionCodes.TransactionsView)))
        {
            throw new NotFoundException("Payment not found.");
        }
        return transaction;
    }

    private async Task<Course> PublishedCourseAsync(string slug, CancellationToken ct) =>
        (await courses.CatalogQuery().Where(c => c.Slug == slug && c.IsPublished).ToListAsync(ct)).FirstOrDefault()
        ?? throw new NotFoundException("Course not found.");

    private async Task<string> NewTransactionNoAsync(string prefix, CancellationToken ct)
    {
        while (true)
        {
            var suffix = string.Concat(RandomNumberGenerator.GetBytes(6).Select(b => CodeAlphabet[b % CodeAlphabet.Length]));
            var number = $"{prefix}-{Now:yyyyMMdd}-{suffix}";
            if (!await payments.NumberExistsAsync(number, ct)) return number;
        }
    }

    private static GatewayPaymentRequest ToGatewayRequest(PaymentTransaction t, string methodCode) =>
        new(t.TransactionNo, t.Amount, "BDT", methodCode, t.CourseTitle, t.UserName, t.UserEmail, t.ExpiresAt);

    private static bool IsExternal(string url) => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private async Task<TransactionDto> ToDtoAsync(PaymentTransaction t, bool admin, CancellationToken ct)
    {
        var canOpen = StatusOf(t) == PaymentStatusCodes.Success
            && (await enrollmentRepository.ActiveCourseIdsAsync(t.UserId, ct)).Contains(t.CourseId)
            && t.UserId == currentUser.UserId;
        return ToDto(t, admin, canOpen);
    }

    private static TransactionDto ToDto(PaymentTransaction t, bool admin, bool canOpenCourse) => new(
        t.TransactionNo, t.Gateway, t.UserId, t.UserName, t.UserEmail, t.CourseId, t.CourseSlug, t.CourseTitle, t.Method.Code, t.MaskedAccount,
        t.OriginalAmount, t.DiscountAmount, t.Amount, "BDT", StatusOf(t), t.FailureReason, t.FulfilledAt, t.ExpiresAt, t.CreatedAt, t.UpdatedAt,
        admin ? AdminTransitions[StatusOf(t)] : [], canOpenCourse && StatusOf(t) == PaymentStatusCodes.Success);
}
