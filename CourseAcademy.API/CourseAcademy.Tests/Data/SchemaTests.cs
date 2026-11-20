using CourseAcademy.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Data;

/// <summary>Every table lives in the schema of its area, never in dbo.</summary>
public class SchemaTests
{
    [Fact]
    public void Every_table_has_an_area_schema()
    {
        var (db, _) = TestDb.Create();
        var tables = db.Model.GetEntityTypes().Select(e => (Table: e.GetTableName(), Schema: e.GetSchema())).ToList();

        Assert.All(tables, t => Assert.Contains(t.Schema, new[]
        {
            DbSchemas.Securities, DbSchemas.Lookups, DbSchemas.Catalog, DbSchemas.Commerce, DbSchemas.Learning, DbSchemas.Media,
            DbSchemas.Certification, DbSchemas.Community,
        }));
        Assert.Contains(("User", DbSchemas.Securities), tables);
        Assert.Contains(("Section", DbSchemas.Catalog), tables);
        Assert.Contains(("PaymentTransaction", DbSchemas.Commerce), tables);
        Assert.Contains(("Certificate", DbSchemas.Certification), tables);
        Assert.Contains(("Comment", DbSchemas.Community), tables);
        Assert.Contains(("ExamType", DbSchemas.Lookups), tables);
    }
}
