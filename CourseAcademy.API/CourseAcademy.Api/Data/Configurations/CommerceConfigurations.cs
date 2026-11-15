using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ConfigureBase();
        builder.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.CourseId, e.EnrollmentStatusId });
        builder.HasIndex(e => e.PaymentTransactionId);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.EnrollmentStatusId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.PaymentTransaction).WithMany().HasForeignKey(e => e.PaymentTransactionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.TransactionNo).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Gateway).HasMaxLength(50).IsRequired();
        builder.Property(e => e.GatewayReference).HasMaxLength(200);
        builder.Property(e => e.UserName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UserEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.CourseSlug).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CourseTitle).HasMaxLength(120).IsRequired();
        builder.Property(e => e.MaskedAccount).HasMaxLength(50);
        builder.Property(e => e.FailureReason).HasMaxLength(200);
        // Two requests finishing the same payment at once: only the first status change is saved.
        builder.Property(e => e.PaymentStatusId).IsConcurrencyToken();

        builder.HasIndex(e => e.TransactionNo).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });
        builder.HasIndex(e => e.PaymentStatusId);
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.PaymentStatusId, e.ExpiresAt });
        builder.HasIndex(e => new { e.Gateway, e.GatewayReference }).HasFilter("[GatewayReference] IS NOT NULL");

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        // The course row is only ever soft-deleted, so the reference stays valid.
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Method).WithMany().HasForeignKey(e => e.PaymentMethodId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Currency).WithMany().HasForeignKey(e => e.CurrencyId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.PaymentStatusId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_PaymentTransaction_Amounts",
            "[OriginalAmount] >= 0 AND [DiscountAmount] >= 0 AND [Amount] >= 0 AND [Amount] + [DiscountAmount] = [OriginalAmount]"));
    }
}
