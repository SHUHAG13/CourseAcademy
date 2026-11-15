using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;

namespace CourseAcademy.Tests.Data;

public class RepositoryTests
{
    [Fact]
    public async Task Changes_are_written_only_by_the_unit_of_work()
    {
        var (db, _) = TestDb.Create();
        var repository = new Repository<CourseBadge>(db);
        var unitOfWork = new UnitOfWork(db);

        await repository.AddAsync(new CourseBadge { Code = "Trending", Name = "Trending" }, CancellationToken.None);
        Assert.False(await repository.AnyAsync(b => b.Code == "Trending", CancellationToken.None));

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        Assert.True(await repository.AnyAsync(b => b.Code == "Trending", CancellationToken.None));
    }

    [Fact]
    public async Task Removed_entities_are_not_returned()
    {
        var (db, _) = TestDb.Create();
        var repository = new Repository<CourseFormat>(db);
        var live = (await repository.ListAsync(f => f.Code == "Live", CancellationToken.None)).Single();

        repository.Remove((await repository.GetByIdAsync(live.Id, CancellationToken.None))!);
        await new UnitOfWork(db).SaveChangesAsync(CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(live.Id, CancellationToken.None));
        Assert.Equal(2, (await repository.ListAsync(CancellationToken.None)).Count);
        Assert.Equal(0, await repository.CountAsync(f => f.Code == "Live", CancellationToken.None));
    }

    [Fact]
    public async Task Lookup_repository_lists_in_sort_order_and_finds_by_code()
    {
        var (db, _) = TestDb.Create();
        var lookups = new LookupRepository(db);

        var statuses = await lookups.ListAsync<PaymentStatus>(CancellationToken.None);
        var refunded = await lookups.FindByCodeAsync<PaymentStatus>("refunded", CancellationToken.None);

        Assert.Equal(["pending", "success", "failed", "cancelled", "refunded"], statuses.Select(s => s.Code));
        Assert.Equal("Refunded", refunded?.Name);
        Assert.Null(await lookups.FindByCodeAsync<PaymentStatus>("unknown", CancellationToken.None));
    }
}
