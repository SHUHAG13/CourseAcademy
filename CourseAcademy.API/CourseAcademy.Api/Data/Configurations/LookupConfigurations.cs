using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

/// <summary>Shared table shape for lookups: unique Code among non-deleted rows, plus the seed values.</summary>
public abstract class LookupConfiguration<T> : IEntityTypeConfiguration<T> where T : LookupEntity
{
    protected abstract IEnumerable<T> Seed { get; }

    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ToTable(typeof(T).Name);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => e.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasData(Seed);
    }
}

public sealed class CourseFormatConfiguration : LookupConfiguration<CourseFormat>
{
    protected override IEnumerable<CourseFormat> Seed => LookupSeed.CourseFormats;
}

public sealed class CourseLevelConfiguration : LookupConfiguration<CourseLevel>
{
    protected override IEnumerable<CourseLevel> Seed => LookupSeed.CourseLevels;
}

public sealed class CourseBadgeConfiguration : LookupConfiguration<CourseBadge>
{
    protected override IEnumerable<CourseBadge> Seed => LookupSeed.CourseBadges;
}

public sealed class DocumentTypeConfiguration : LookupConfiguration<DocumentType>
{
    protected override IEnumerable<DocumentType> Seed => LookupSeed.DocumentTypes;

    public override void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.FileExtension).HasMaxLength(10).IsRequired();
    }
}

public sealed class MaterialTypeConfiguration : LookupConfiguration<MaterialType>
{
    protected override IEnumerable<MaterialType> Seed => LookupSeed.MaterialTypes;
}

public sealed class EnrollmentStatusConfiguration : LookupConfiguration<EnrollmentStatus>
{
    protected override IEnumerable<EnrollmentStatus> Seed => LookupSeed.EnrollmentStatuses;
}

public sealed class PaymentStatusConfiguration : LookupConfiguration<PaymentStatus>
{
    protected override IEnumerable<PaymentStatus> Seed => LookupSeed.PaymentStatuses;
}

public sealed class CurrencyConfiguration : LookupConfiguration<Currency>
{
    protected override IEnumerable<Currency> Seed => LookupSeed.Currencies;
}

public sealed class CategoryConfiguration : LookupConfiguration<Category>
{
    protected override IEnumerable<Category> Seed => LookupSeed.Categories;

    public override void Configure(EntityTypeBuilder<Category> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.ShortName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ImageUrl).HasMaxLength(2048).IsRequired();
    }
}

public sealed class PaymentMethodConfiguration : LookupConfiguration<PaymentMethod>
{
    protected override IEnumerable<PaymentMethod> Seed => LookupSeed.PaymentMethods;

    public override void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Description).HasMaxLength(200).IsRequired();
        builder.Property(e => e.LogoUrl).HasMaxLength(2048);
        builder.Property(e => e.Icon).HasMaxLength(50).IsRequired();
    }
}
