namespace CourseAcademy.Api.Dtos;

public sealed record LookupDto(string Code, string Name);

public sealed record DocumentTypeDto(string Code, string Name, string FileExtension);

public sealed record CategoryDto(string Code, string Name, string ShortName, string Description, string Icon, string ImageUrl);

public sealed record PaymentMethodDto(string Code, string Name, string Description, string? LogoUrl, string Icon);

/// <summary>All fixed value lists the frontend needs for forms and filters, in display order.</summary>
public sealed record LookupsDto(
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<LookupDto> CourseFormats,
    IReadOnlyList<LookupDto> CourseLevels,
    IReadOnlyList<LookupDto> CourseBadges,
    IReadOnlyList<DocumentTypeDto> DocumentTypes,
    IReadOnlyList<LookupDto> MaterialTypes,
    IReadOnlyList<LookupDto> EnrollmentStatuses,
    IReadOnlyList<LookupDto> PaymentStatuses,
    IReadOnlyList<PaymentMethodDto> PaymentMethods,
    IReadOnlyList<LookupDto> Currencies);
