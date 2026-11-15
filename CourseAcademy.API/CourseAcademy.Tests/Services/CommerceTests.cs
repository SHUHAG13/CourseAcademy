using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Services;

/// <summary>Checkout, the demo gateway, payment rules and enrollments.</summary>
public class CommerceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    /// <summary>A kit with one published paid course and one published free course, signed in as the demo student.</summary>
    private static async Task<(CatalogTests.Kit Kit, CourseDetailDto Paid, CourseDetailDto Free)> SetUp()
    {
        var kit = new CatalogTests.Kit().As(IdentityTestKit.Admin);
        var paid = await Publish(kit, kit.Course("paid-course") with { OriginalPrice = 8000, Price = 4999 });
        var free = await Publish(kit, kit.Course("free-course") with { OriginalPrice = 0, Price = 0 });
        kit.As(IdentityTestKit.Student);
        return (kit, paid, free);
    }

    private static async Task<CourseDetailDto> Publish(CatalogTests.Kit kit, CourseUpsertRequest request)
    {
        var course = await kit.Admin.CreateAsync(request, Ct);
        await kit.Admin.SaveCurriculumAsync(course.Id, CatalogTests.TwoSections(), Ct);
        return await kit.Admin.SetPublishedAsync(course.Id, true, Ct);
    }

    [Fact]
    public async Task Starting_a_payment_takes_the_price_from_the_course_and_sends_the_buyer_to_the_gateway()
    {
        var (kit, paid, _) = await SetUp();

        var started = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);
        var payment = await kit.Payments.GetAsync(started.TransactionNo, Ct);

        Assert.StartsWith("DEMO-20261114-", started.TransactionNo);
        Assert.Equal($"/checkout/pay/{started.TransactionNo}", started.RedirectUrl);
        Assert.False(started.IsExternal);
        Assert.Equal(PaymentStatusCodes.Pending, payment.StatusCode);
        Assert.Equal((8000m, 3001m, 4999m), (payment.OriginalAmount, payment.DiscountAmount, payment.Amount));
        Assert.Equal("017•••••000", payment.MaskedAccount);
        Assert.Equal(TestDb.Start.UtcDateTime.AddMinutes(30), payment.ExpiresAt);
    }

    [Fact]
    public async Task The_same_method_continues_the_open_payment_and_another_method_replaces_it()
    {
        var (kit, paid, _) = await SetUp();
        var first = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);

        var again = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);
        var other = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "nagad"), Ct);

        Assert.Equal(first.TransactionNo, again.TransactionNo);
        Assert.NotEqual(first.TransactionNo, other.TransactionNo);
        Assert.Equal(PaymentStatusCodes.Cancelled, (await kit.Payments.GetAsync(first.TransactionNo, Ct)).StatusCode);
        Assert.Equal(PaymentStatusCodes.Pending, (await kit.Payments.GetAsync(other.TransactionNo, Ct)).StatusCode);
    }

    [Fact]
    public async Task A_successful_demo_payment_grants_access_once()
    {
        var (kit, paid, _) = await SetUp();
        var started = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "card"), Ct);

        var done = await kit.Payments.CompleteDemoPaymentAsync(started.TransactionNo, new DemoOutcomeRequest("success"), Ct);
        var repeated = await kit.Payments.CompleteDemoPaymentAsync(started.TransactionNo, new DemoOutcomeRequest("failed"), Ct);

        Assert.Equal(PaymentStatusCodes.Success, done.StatusCode);
        Assert.True(done.CanOpenCourse);
        Assert.Equal(PaymentStatusCodes.Success, repeated.StatusCode);
        var enrollment = Assert.Single(await kit.Enrollments.GetMineAsync(Ct));
        Assert.Equal(("active", started.TransactionNo), (enrollment.StatusCode, enrollment.TransactionNo));
        Assert.True((await kit.Catalog.GetBySlugAsync(paid.Slug, Ct)).HasContentAccess);
        await Assert.ThrowsAsync<ConflictException>(() => kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "card"), Ct));
    }

    [Fact]
    public async Task A_failed_demo_payment_records_the_reason_and_grants_nothing()
    {
        var (kit, paid, _) = await SetUp();
        var started = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "rocket"), Ct);

        var failed = await kit.Payments.CompleteDemoPaymentAsync(started.TransactionNo, new DemoOutcomeRequest("failed"), Ct);

        Assert.Equal(PaymentStatusCodes.Failed, failed.StatusCode);
        Assert.Equal("Transaction limit exceeded (simulated)", failed.FailureReason);
        Assert.Empty(await kit.Enrollments.GetMineAsync(Ct));
        Assert.False((await kit.Catalog.GetBySlugAsync(paid.Slug, Ct)).HasContentAccess);
    }

    [Fact]
    public async Task Other_students_cannot_see_or_finish_a_payment()
    {
        var (kit, paid, _) = await SetUp();
        var started = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);

        kit.As(IdentityTestKit.Learner);

        await Assert.ThrowsAsync<NotFoundException>(() => kit.Payments.GetAsync(started.TransactionNo, Ct));
        await Assert.ThrowsAsync<NotFoundException>(() => kit.Payments.CompleteDemoPaymentAsync(started.TransactionNo, new DemoOutcomeRequest("success"), Ct));
    }

    [Fact]
    public async Task Pending_payments_are_cancelled_after_30_minutes()
    {
        var (kit, paid, _) = await SetUp();
        var first = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);
        var second = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);
        kit.Identity.Clock.Advance(TimeSpan.FromMinutes(31));

        var expiredCount = await kit.Payments.ExpireStaleAsync(Ct);
        var expired = await kit.Payments.GetAsync(first.TransactionNo, Ct);
        var tooLate = await kit.Payments.CompleteDemoPaymentAsync(first.TransactionNo, new DemoOutcomeRequest("success"), Ct);

        Assert.Equal(first.TransactionNo, second.TransactionNo);
        Assert.Equal(1, expiredCount);
        Assert.Equal((PaymentStatusCodes.Cancelled, PaymentService.ExpiredReason), (expired.StatusCode, expired.FailureReason));
        Assert.Equal(PaymentStatusCodes.Cancelled, tooLate.StatusCode);
        Assert.Empty(await kit.Enrollments.GetMineAsync(Ct));
    }

    [Fact]
    public async Task Admins_may_only_make_the_allowed_status_changes_and_refunds_remove_access()
    {
        var (kit, paid, _) = await SetUp();
        var started = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);
        kit.As(IdentityTestKit.Admin);

        var paidByAdmin = await kit.Payments.SetStatusAsync(started.TransactionNo, PaymentStatusCodes.Success, Ct);
        Assert.Equal([PaymentStatusCodes.Refunded], paidByAdmin.AllowedActions);
        await Assert.ThrowsAsync<ConflictException>(() => kit.Payments.DeleteAsync(started.TransactionNo, Ct));

        await kit.Payments.SetStatusAsync(started.TransactionNo, PaymentStatusCodes.Refunded, Ct);
        await Assert.ThrowsAsync<ConflictException>(() => kit.Payments.SetStatusAsync(started.TransactionNo, PaymentStatusCodes.Success, Ct));

        kit.As(IdentityTestKit.Student);
        Assert.Equal("revoked", Assert.Single(await kit.Enrollments.GetMineAsync(Ct)).StatusCode);
        Assert.False((await kit.Catalog.GetBySlugAsync(paid.Slug, Ct)).HasContentAccess);

        kit.As(IdentityTestKit.Admin);
        await kit.Payments.DeleteAsync(started.TransactionNo, Ct);
        Assert.Equal(0, (await kit.Payments.GetAllAsync(new TransactionListQuery(), Ct)).Total);
    }

    [Fact]
    public async Task Students_cannot_manage_payments_or_enrollments()
    {
        var (kit, paid, _) = await SetUp();
        var started = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);

        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Payments.GetAllAsync(new TransactionListQuery(), Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Payments.SetStatusAsync(started.TransactionNo, PaymentStatusCodes.Success, Ct));
        await Assert.ThrowsAsync<ForbiddenException>(() => kit.Enrollments.GetAllAsync(new EnrollmentListQuery(), Ct));
    }

    [Fact]
    public async Task Free_courses_enroll_directly_and_paid_ones_never_do()
    {
        var (kit, paid, free) = await SetUp();

        var enrollment = await kit.Enrollments.EnrollFreeAsync(free.Slug, Ct);

        Assert.Equal(("active", (string?)null), (enrollment.StatusCode, enrollment.TransactionNo));
        await Assert.ThrowsAsync<ConflictException>(() => kit.Enrollments.EnrollFreeAsync(free.Slug, Ct));
        await Assert.ThrowsAsync<ConflictException>(() => kit.Enrollments.EnrollFreeAsync(paid.Slug, Ct));
        await Assert.ThrowsAsync<ConflictException>(() => kit.Payments.StartAsync(new StartPaymentRequest(free.Slug, "bkash"), Ct));
    }

    [Fact]
    public async Task Admin_list_filters_and_totals_cover_every_matching_payment()
    {
        var (kit, paid, _) = await SetUp();
        var success = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "bkash"), Ct);
        await kit.Payments.CompleteDemoPaymentAsync(success.TransactionNo, new DemoOutcomeRequest("success"), Ct);
        kit.As(IdentityTestKit.Learner);
        var failed = await kit.Payments.StartAsync(new StartPaymentRequest(paid.Slug, "card"), Ct);
        await kit.Payments.CompleteDemoPaymentAsync(failed.TransactionNo, new DemoOutcomeRequest("failed"), Ct);
        kit.As(IdentityTestKit.Admin);

        var all = await kit.Payments.GetAllAsync(new TransactionListQuery { PageSize = 1 }, Ct);
        var cards = await kit.Payments.GetAllAsync(new TransactionListQuery { Method = "card" }, Ct);
        var today = await kit.Payments.GetAllAsync(new TransactionListQuery { From = "2026-11-14", To = "2026-11-14" }, Ct);
        var yesterday = await kit.Payments.GetAllAsync(new TransactionListQuery { To = "2026-11-13" }, Ct);

        Assert.Equal((2, 1), (all.Total, all.Items.Count));
        Assert.Equal(new TransactionTotalsDto(4999, 0, 1, 1), all.Totals);
        Assert.Equal([failed.TransactionNo], cards.Items.Select(t => t.TransactionNo));
        Assert.Equal(2, today.Total);
        Assert.Equal(0, yesterday.Total);
        Assert.Equal([paid.Title], all.CourseOptions.Select(o => o.Title));
    }

    [Fact]
    public async Task Admins_revoke_and_restore_access_and_deleting_a_course_removes_its_enrollments()
    {
        var (kit, paid, free) = await SetUp();
        var enrollment = await kit.Enrollments.EnrollFreeAsync(free.Slug, Ct);
        kit.As(IdentityTestKit.Admin);

        var revoked = await kit.Enrollments.SetStatusAsync(enrollment.Id, "revoked", Ct);
        var restored = await kit.Enrollments.SetStatusAsync(enrollment.Id, "active", Ct);
        Assert.Equal(("revoked", "active"), (revoked.StatusCode, restored.StatusCode));

        await kit.Admin.DeleteAsync(free.Id, Ct);
        Assert.Equal(0, (await kit.Enrollments.GetAllAsync(new EnrollmentListQuery(), Ct)).Total);
        Assert.True(await kit.Identity.Db.Enrollments.IgnoreQueryFilters().AnyAsync(e => e.Id == enrollment.Id && e.IsDeleted));
        Assert.Equal(1, (await kit.Catalog.GetAllAsync(Ct)).Single(c => c.Id == paid.Id).ActiveEnrollmentCount + 1);
    }
}
