using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Common;

public static class DbErrors
{
    /// <summary>True when SQL Server refused the save because of a unique index or constraint (errors 2601, 2627).</summary>
    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
