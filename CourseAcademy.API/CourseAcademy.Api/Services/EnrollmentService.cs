using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Services;

/// <summary>Ids of fixed lookup rows. They are seeded with fixed ids, so they can be used without a query.</summary>
public static class LookupIds
{
    public static int PaymentStatus(string code) => LookupSeed.PaymentStatuses.Single(s => s.Code == code).Id;
    public static int EnrollmentStatus(string code) => LookupSeed.EnrollmentStatuses.Single(s => s.Code == code).Id;
    public static int Currency(string code) => LookupSeed.Currencies.Single(s => s.Code == code).Id;
}

public static class EnrollmentStatusCodes
{
    public const string Active = "active";
    public const string Revoked = "revoked";
}

public interface IEnrollmentService
{
    /// <summary>The signed-in user's enrollments, newest first, including revoked ones.</summary>
    Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(CancellationToken ct);

    Task<PagedResult<EnrollmentDto>> GetAllAsync(EnrollmentListQuery query, CancellationToken ct);

    Task<EnrollmentDto> SetStatusAsync(int enrollmentId, string statusCode, CancellationToken ct);

    /// <summary>Enrolls the signed-in user in a free published course (BR-PAY-03).</summary>
    Task<EnrollmentDto> EnrollFreeAsync(string courseSlug, CancellationToken ct);

    /// <summary>
    /// Activates a course for a user, re-using their existing row (BR-ENR-01). Only adds to the change tracker;
    /// the caller saves, so the grant is stored together with whatever caused it.
    /// </summary>
    Task GrantAsync(int userId, int courseId, PaymentTransaction? payment, DateTime now, CancellationToken ct);

    /// <summary>Revokes the access a payment granted (refund, BR-ENR-06). The caller saves.</summary>
    Task RevokeForPaymentAsync(int paymentTransactionId, CancellationToken ct);
}

public sealed class EnrollmentService(
    IEnrollmentRepository enrollments,
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IEnrollmentService
{
    public async Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var rows = await enrollments.ListQuery().Where(e => e.UserId == userId).OrderByDescending(e => e.EnrolledAt).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<PagedResult<EnrollmentDto>> GetAllAsync(EnrollmentListQuery q, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.EnrollmentsView, null, ct);
        var query = enrollments.ListQuery();
        if (!string.IsNullOrWhiteSpace(q.Status)) query = query.Where(e => e.Status.Code == q.Status);
        var term = q.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(e => e.User.Name.Contains(term) || e.User.Email.Contains(term) || e.Course.Title.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(e => e.EnrolledAt).ThenByDescending(e => e.Id).Skip(q.Skip).Take(q.PageSize).ToListAsync(ct);
        return new PagedResult<EnrollmentDto>(rows.Select(ToDto).ToList(), total, q.Page, q.PageSize);
    }

    public async Task<EnrollmentDto> SetStatusAsync(int enrollmentId, string statusCode, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.EnrollmentsEdit, null, ct);
        if (statusCode is not (EnrollmentStatusCodes.Active or EnrollmentStatusCodes.Revoked))
        {
            throw new BusinessRuleException("Choose active or revoked.", "statusCode");
        }
        var enrollment = await enrollments.GetByIdAsync(enrollmentId, ct) ?? throw new NotFoundException("Enrollment not found.");
        enrollment.EnrollmentStatusId = LookupIds.EnrollmentStatus(statusCode);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(await enrollments.ListQuery().SingleAsync(e => e.Id == enrollmentId, ct));
    }

    public async Task<EnrollmentDto> EnrollFreeAsync(string courseSlug, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CheckoutView, null, ct);
        var userId = currentUser.RequireUserId();
        var course = (await courses.ListAsync(c => c.Slug == courseSlug && c.IsPublished, ct)).FirstOrDefault()
            ?? throw new NotFoundException("Course not found.");
        if (course.Price > 0) throw new ConflictException("This course is not free. Please pay to enroll.");

        var existing = await enrollments.GetForUserCourseAsync(userId, course.Id, ct);
        if (existing?.Status.Code == EnrollmentStatusCodes.Active) throw new ConflictException("You're already enrolled in this course.");

        await GrantAsync(userId, course.Id, null, clock.GetUtcNow().UtcDateTime, ct);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            // A second click at the same moment already enrolled the user.
            throw new ConflictException("You're already enrolled in this course.");
        }
        return ToDto(await enrollments.ListQuery().SingleAsync(e => e.UserId == userId && e.CourseId == course.Id, ct));
    }

    public async Task GrantAsync(int userId, int courseId, PaymentTransaction? payment, DateTime now, CancellationToken ct)
    {
        var enrollment = await enrollments.GetForUserCourseAsync(userId, courseId, ct);
        if (enrollment is null)
        {
            enrollment = new Enrollment { UserId = userId, CourseId = courseId };
            await enrollments.AddAsync(enrollment, ct);
        }
        enrollment.EnrollmentStatusId = LookupIds.EnrollmentStatus(EnrollmentStatusCodes.Active);
        enrollment.EnrolledAt = now;
        enrollment.PaymentTransaction = payment;
        enrollment.PaymentTransactionId = payment?.Id;
    }

    public async Task RevokeForPaymentAsync(int paymentTransactionId, CancellationToken ct)
    {
        foreach (var enrollment in await enrollments.ListByPaymentAsync(paymentTransactionId, ct))
        {
            enrollment.EnrollmentStatusId = LookupIds.EnrollmentStatus(EnrollmentStatusCodes.Revoked);
        }
    }

    private static EnrollmentDto ToDto(Enrollment e) => new(
        e.Id, e.UserId, e.User.Name, e.User.Email, e.CourseId, e.Course.Title, e.Status.Code, e.EnrolledAt, e.PaymentTransaction?.TransactionNo);
}
