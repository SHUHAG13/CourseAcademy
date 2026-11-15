using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;

namespace CourseAcademy.Tests.Services;

public class LookupServiceTests
{
    [Fact]
    public async Task Returns_every_seeded_list_with_codes_the_frontend_uses()
    {
        var (db, _) = TestDb.Create();
        var service = new LookupService(new LookupRepository(db));

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(["video-editing", "digital-marketing", "sports-journalism", "software-development"], result.Categories.Select(c => c.Code));
        Assert.Equal(["Pre-recorded", "Live", "Hybrid"], result.CourseFormats.Select(f => f.Code));
        Assert.Equal(["Beginner", "Intermediate", "Advanced"], result.CourseLevels.Select(l => l.Code));
        Assert.Equal(["bkash", "nagad", "rocket", "card"], result.PaymentMethods.Select(m => m.Code));
        Assert.Equal(["active", "revoked"], result.EnrollmentStatuses.Select(s => s.Code));
        Assert.Equal(".pptx", result.DocumentTypes.Single(d => d.Code == "PPTX").FileExtension);
        Assert.Equal("BDT", result.Currencies.Single().Code);
    }

    [Fact]
    public async Task Hides_soft_deleted_values()
    {
        var (db, _) = TestDb.Create();
        db.Remove(db.CourseBadges.Single(b => b.Code == "New"));
        await db.SaveChangesAsync();

        var result = await new LookupService(new LookupRepository(db)).GetAllAsync(CancellationToken.None);

        Assert.Equal(["Bestseller", "Popular"], result.CourseBadges.Select(b => b.Code));
    }
}
