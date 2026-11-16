using System.Linq.Expressions;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

public interface ICourseRepository : IRepository<Course>
{
    /// <summary>
    /// Courses matching <paramref name="filter"/> with lookups, instructor, outcomes, requirements, modules,
    /// submodules, sessions and materials. Sections are loaded separately with <see cref="ListSectionsAsync"/>.
    /// </summary>
    Task<List<Course>> ListWithDetailsAsync(Expression<Func<Course, bool>> filter, bool tracking, CancellationToken ct);

    /// <summary>Every section and topic of the given courses, with videos and documents and their uploaded files.</summary>
    Task<List<Section>> ListSectionsAsync(IReadOnlyCollection<int> courseIds, bool tracking, CancellationToken ct);

    /// <summary>Number of sections and topics per course.</summary>
    Task<Dictionary<int, int>> CountSectionsAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct);

    Task<bool> SlugExistsAsync(string slug, int? exceptId, CancellationToken ct);

    /// <summary>Courses for the catalogue list, with the lookups and instructor needed for cards. Filtering is done by the caller.</summary>
    IQueryable<Course> CatalogQuery();
}

public sealed class CourseRepository(AppDbContext db) : Repository<Course>(db), ICourseRepository
{
    public Task<List<Course>> ListWithDetailsAsync(Expression<Func<Course, bool>> filter, bool tracking, CancellationToken ct)
    {
        IQueryable<Course> query = tracking ? Set : Set.AsNoTracking();
        return query.Where(filter)
            .Include(c => c.Category)
            .Include(c => c.Format)
            .Include(c => c.Level)
            .Include(c => c.Badge)
            .Include(c => c.Instructor).ThenInclude(i => i.Expertise)
            .Include(c => c.Outcomes)
            .Include(c => c.Requirements)
            .Include(c => c.Modules).ThenInclude(m => m.Submodules)
            .Include(c => c.Sessions)
            .Include(c => c.Materials).ThenInclude(m => m.MaterialType)
            .Include(c => c.Materials).ThenInclude(m => m.Asset)
            .AsSplitQuery()
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
    }

    public Task<List<Section>> ListSectionsAsync(IReadOnlyCollection<int> courseIds, bool tracking, CancellationToken ct)
    {
        IQueryable<Section> query = tracking ? Db.Sections : Db.Sections.AsNoTracking();
        return query.Where(s => courseIds.Contains(s.CourseId))
            .Include(s => s.Videos).ThenInclude(v => v.Asset)
            .Include(s => s.Documents).ThenInclude(d => d.DocumentType)
            .Include(s => s.Documents).ThenInclude(d => d.Asset)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public async Task<Dictionary<int, int>> CountSectionsAsync(IReadOnlyCollection<int> courseIds, CancellationToken ct) =>
        await Db.Sections.Where(s => courseIds.Contains(s.CourseId))
            .GroupBy(s => s.CourseId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public Task<bool> SlugExistsAsync(string slug, int? exceptId, CancellationToken ct) =>
        Set.AnyAsync(c => c.Slug == slug && c.Id != exceptId, ct);

    public IQueryable<Course> CatalogQuery() =>
        Set.AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Format)
            .Include(c => c.Level)
            .Include(c => c.Badge)
            .Include(c => c.Instructor);
}

public interface IInstructorRepository : IRepository<Instructor>
{
    Task<List<Instructor>> ListWithExpertiseAsync(CancellationToken ct);

    /// <summary>Tracked instructor with expertise.</summary>
    Task<Instructor?> GetWithExpertiseAsync(int id, CancellationToken ct);

    /// <summary>Number of courses (published or not) that the instructor teaches.</summary>
    Task<int> CountCoursesAsync(int instructorId, CancellationToken ct);
}

public sealed class InstructorRepository(AppDbContext db) : Repository<Instructor>(db), IInstructorRepository
{
    public Task<List<Instructor>> ListWithExpertiseAsync(CancellationToken ct) =>
        Query.Include(i => i.Expertise).OrderBy(i => i.Id).ToListAsync(ct);

    public Task<Instructor?> GetWithExpertiseAsync(int id, CancellationToken ct) =>
        Set.Include(i => i.Expertise).FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<int> CountCoursesAsync(int instructorId, CancellationToken ct) =>
        Db.Courses.CountAsync(c => c.InstructorId == instructorId, ct);
}
