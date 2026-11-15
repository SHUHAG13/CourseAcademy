using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Data;

public class AuditInterceptorTests
{
    [Fact]
    public async Task Sets_created_at_in_utc_on_insert()
    {
        var (db, _) = TestDb.Create();
        var badge = new CourseBadge { Code = "Trending", Name = "Trending", CreatedAt = DateTime.MinValue, IsDeleted = true };

        db.Add(badge);
        await db.SaveChangesAsync();

        Assert.Equal(TestDb.Start.UtcDateTime, badge.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, badge.CreatedAt.Kind);
        Assert.Null(badge.UpdatedAt);
        Assert.False(badge.IsDeleted);
    }

    [Fact]
    public async Task Sets_updated_at_and_keeps_created_at_on_update()
    {
        var (db, clock) = TestDb.Create();
        var badge = new CourseBadge { Code = "Trending", Name = "Trending" };
        db.Add(badge);
        await db.SaveChangesAsync();

        clock.Advance(TimeSpan.FromHours(2));
        badge.Name = "Hot";
        badge.CreatedAt = DateTime.UtcNow.AddYears(-5);
        await db.SaveChangesAsync();

        var stored = await db.CourseBadges.AsNoTracking().SingleAsync(b => b.Id == badge.Id);
        Assert.Equal(TestDb.Start.UtcDateTime, stored.CreatedAt);
        Assert.Equal(TestDb.Start.UtcDateTime.AddHours(2), stored.UpdatedAt);
    }

    [Fact]
    public async Task Delete_is_soft_and_hidden_from_queries()
    {
        var (db, _) = TestDb.Create();
        var level = await db.CourseLevels.SingleAsync(l => l.Code == "Advanced");

        db.Remove(level);
        await db.SaveChangesAsync();

        Assert.False(await db.CourseLevels.AnyAsync(l => l.Code == "Advanced"));
        var stored = await db.CourseLevels.IgnoreQueryFilters().SingleAsync(l => l.Code == "Advanced");
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.UpdatedAt);
    }
}
