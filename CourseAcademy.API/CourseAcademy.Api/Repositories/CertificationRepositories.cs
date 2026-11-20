using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

public interface ICertificationRuleRepository : IRepository<CertificationRule>
{
    /// <summary>Tracked rule of one course, or null when the course uses the default rule.</summary>
    Task<CertificationRule?> GetForCourseAsync(int courseId, CancellationToken ct);

    /// <summary>Read-only rules of the given courses, by course id.</summary>
    Task<Dictionary<int, CertificationRule>> ListForCoursesAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct);
}

public sealed class CertificationRuleRepository(AppDbContext db) : Repository<CertificationRule>(db), ICertificationRuleRepository
{
    public Task<CertificationRule?> GetForCourseAsync(int courseId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(r => r.CourseId == courseId, ct);

    public Task<Dictionary<int, CertificationRule>> ListForCoursesAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct) =>
        Query.Where(r => courseIds.Contains(r.CourseId)).ToDictionaryAsync(r => r.CourseId, ct);
}

public interface IExamSubmissionRepository : IRepository<ExamSubmission>
{
    /// <summary>Read-only hand-ins of one user in one course, newest first.</summary>
    Task<List<ExamSubmission>> ListForUserCourseAsync(int userId, int courseId, CancellationToken ct);

    /// <summary>
    /// Read-only hand-ins of courses that still exist, for lists; filtering, sorting and paging are done by the caller.
    /// </summary>
    IQueryable<ExamSubmission> ListQuery();
}

public sealed class ExamSubmissionRepository(AppDbContext db) : Repository<ExamSubmission>(db), IExamSubmissionRepository
{
    public Task<List<ExamSubmission>> ListForUserCourseAsync(int userId, int courseId, CancellationToken ct) =>
        Query.Where(s => s.UserId == userId && s.CourseId == courseId)
            .OrderByDescending(s => s.SubmittedAt).ThenByDescending(s => s.Id)
            .ToListAsync(ct);

    public IQueryable<ExamSubmission> ListQuery() => Query.Where(s => Db.Courses.Any(c => c.Id == s.CourseId));
}

public interface ICertificateRepository : IRepository<Certificate>
{
    /// <summary>Read-only certificates (valid and revoked) of one user in one course, newest first.</summary>
    Task<List<Certificate>> ListForUserCourseAsync(int userId, int courseId, CancellationToken ct);

    /// <summary>Read-only certificate with this code (already trimmed and upper-cased).</summary>
    Task<Certificate?> GetByCodeAsync(string code, CancellationToken ct);

    /// <summary>Whether any certificate, deleted ones included, already uses the code.</summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>Read-only certificates for lists; filtering, sorting and paging are done by the caller.</summary>
    IQueryable<Certificate> ListQuery();
}

public sealed class CertificateRepository(AppDbContext db) : Repository<Certificate>(db), ICertificateRepository
{
    public Task<List<Certificate>> ListForUserCourseAsync(int userId, int courseId, CancellationToken ct) =>
        Query.Where(c => c.UserId == userId && c.CourseId == courseId)
            .OrderByDescending(c => c.IssuedAt).ThenByDescending(c => c.Id)
            .ToListAsync(ct);

    public Task<Certificate?> GetByCodeAsync(string code, CancellationToken ct) => Query.FirstOrDefaultAsync(c => c.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => Set.IgnoreQueryFilters().AnyAsync(c => c.Code == code, ct);

    public IQueryable<Certificate> ListQuery() => Query;
}
