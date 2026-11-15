namespace CourseAcademy.Api.Entities;

/// <summary>
/// Columns every table has. Timestamps are UTC and set by the database interceptor, not by callers.
/// Deleting an entity sets <see cref="IsDeleted"/> instead of removing the row, unless it implements <see cref="IHardDelete"/>.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>Marks rows that are physically removed on delete, e.g. join rows and tokens that carry no history.</summary>
public interface IHardDelete;
