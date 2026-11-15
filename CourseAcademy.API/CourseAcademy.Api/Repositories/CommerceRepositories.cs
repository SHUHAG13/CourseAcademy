using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

public interface IPaymentRepository : IRepository<PaymentTransaction>
{
    /// <summary>Tracked payment with method, status and currency.</summary>
    Task<PaymentTransaction?> GetByNoAsync(string transactionNo, CancellationToken ct);

    Task<bool> NumberExistsAsync(string transactionNo, CancellationToken ct);

    /// <summary>Tracked pending payments of one user for one course.</summary>
    Task<List<PaymentTransaction>> ListPendingAsync(int userId, int courseId, CancellationToken ct);

    /// <summary>Tracked pending payments whose time ran out.</summary>
    Task<List<PaymentTransaction>> ListExpiredAsync(DateTime now, int take, CancellationToken ct);

    /// <summary>Read-only payments with method, status and currency, for lists and filters.</summary>
    IQueryable<PaymentTransaction> ListQuery();
}

public sealed class PaymentRepository(AppDbContext db) : Repository<PaymentTransaction>(db), IPaymentRepository
{
    private IQueryable<PaymentTransaction> WithLookups(IQueryable<PaymentTransaction> query) =>
        query.Include(t => t.Method).Include(t => t.Status).Include(t => t.Currency);

    public Task<PaymentTransaction?> GetByNoAsync(string transactionNo, CancellationToken ct) =>
        WithLookups(Set).FirstOrDefaultAsync(t => t.TransactionNo == transactionNo, ct);

    public Task<bool> NumberExistsAsync(string transactionNo, CancellationToken ct) =>
        Set.IgnoreQueryFilters().AnyAsync(t => t.TransactionNo == transactionNo, ct);

    public Task<List<PaymentTransaction>> ListPendingAsync(int userId, int courseId, CancellationToken ct) =>
        WithLookups(Set).Where(t => t.UserId == userId && t.CourseId == courseId && t.Status.Code == "pending").ToListAsync(ct);

    public Task<List<PaymentTransaction>> ListExpiredAsync(DateTime now, int take, CancellationToken ct) =>
        WithLookups(Set).Where(t => t.Status.Code == "pending" && t.ExpiresAt <= now).OrderBy(t => t.ExpiresAt).Take(take).ToListAsync(ct);

    public IQueryable<PaymentTransaction> ListQuery() => WithLookups(Query);
}

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    /// <summary>Tracked enrollment of one user in one course, with its status.</summary>
    Task<Enrollment?> GetForUserCourseAsync(int userId, int courseId, CancellationToken ct);

    /// <summary>Tracked enrollments granted by one payment.</summary>
    Task<List<Enrollment>> ListByPaymentAsync(int paymentTransactionId, CancellationToken ct);

    /// <summary>Tracked enrollments of one course.</summary>
    Task<List<Enrollment>> ListForCourseAsync(int courseId, CancellationToken ct);

    /// <summary>Courses the user may learn in (status active).</summary>
    Task<HashSet<int>> ActiveCourseIdsAsync(int userId, CancellationToken ct);

    /// <summary>Number of active enrollments per course, and of all enrollments per course.</summary>
    Task<Dictionary<int, (int Active, int Total)>> CountByCourseAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct);

    /// <summary>Read-only enrollments with status, user, course and payment, for lists.</summary>
    IQueryable<Enrollment> ListQuery();
}

public sealed class EnrollmentRepository(AppDbContext db) : Repository<Enrollment>(db), IEnrollmentRepository
{
    public Task<Enrollment?> GetForUserCourseAsync(int userId, int courseId, CancellationToken ct) =>
        Set.Include(e => e.Status).FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

    public Task<List<Enrollment>> ListByPaymentAsync(int paymentTransactionId, CancellationToken ct) =>
        Set.Include(e => e.Status).Where(e => e.PaymentTransactionId == paymentTransactionId).ToListAsync(ct);

    public Task<List<Enrollment>> ListForCourseAsync(int courseId, CancellationToken ct) =>
        Set.Where(e => e.CourseId == courseId).ToListAsync(ct);

    public async Task<HashSet<int>> ActiveCourseIdsAsync(int userId, CancellationToken ct) =>
        (await Query.Where(e => e.UserId == userId && e.Status.Code == "active").Select(e => e.CourseId).ToListAsync(ct)).ToHashSet();

    public async Task<Dictionary<int, (int Active, int Total)>> CountByCourseAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct) =>
        (await Query.Where(e => courseIds.Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new { g.Key, Active = g.Count(e => e.Status.Code == "active"), Total = g.Count() })
            .ToListAsync(ct))
        .ToDictionary(x => x.Key, x => (x.Active, x.Total));

    public IQueryable<Enrollment> ListQuery() =>
        Query.Include(e => e.Status).Include(e => e.User).Include(e => e.Course).Include(e => e.PaymentTransaction);
}
