using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;

namespace CourseAcademy.Api.Services;

public interface ILookupService
{
    Task<LookupsDto> GetAllAsync(CancellationToken ct);
}

public sealed class LookupService(ILookupRepository lookups) : ILookupService
{
    public async Task<LookupsDto> GetAllAsync(CancellationToken ct)
    {
        // One DbContext cannot run queries in parallel, so these run one after another.
        var categories = await lookups.ListAsync<Category>(ct);
        var formats = await lookups.ListAsync<CourseFormat>(ct);
        var levels = await lookups.ListAsync<CourseLevel>(ct);
        var badges = await lookups.ListAsync<CourseBadge>(ct);
        var documentTypes = await lookups.ListAsync<DocumentType>(ct);
        var materialTypes = await lookups.ListAsync<MaterialType>(ct);
        var enrollmentStatuses = await lookups.ListAsync<EnrollmentStatus>(ct);
        var paymentStatuses = await lookups.ListAsync<PaymentStatus>(ct);
        var paymentMethods = await lookups.ListAsync<PaymentMethod>(ct);
        var currencies = await lookups.ListAsync<Currency>(ct);

        return new LookupsDto(
            categories.Select(c => new CategoryDto(c.Code, c.Name, c.ShortName, c.Description, c.Icon, c.ImageUrl)).ToList(),
            ToDto(formats),
            ToDto(levels),
            ToDto(badges),
            documentTypes.Select(d => new DocumentTypeDto(d.Code, d.Name, d.FileExtension)).ToList(),
            ToDto(materialTypes),
            ToDto(enrollmentStatuses),
            ToDto(paymentStatuses),
            paymentMethods.Select(m => new PaymentMethodDto(m.Code, m.Name, m.Description, m.LogoUrl, m.Icon)).ToList(),
            ToDto(currencies));
    }

    private static List<LookupDto> ToDto(IEnumerable<LookupEntity> items) => items.Select(i => new LookupDto(i.Code, i.Name)).ToList();
}
