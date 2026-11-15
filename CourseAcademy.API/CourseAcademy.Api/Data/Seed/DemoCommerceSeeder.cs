using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Adds the frontend's demo payments and enrollments (demo-records.data.ts) when no payment exists yet,
/// so the dashboards and admin lists have something to show. Development only.
/// </summary>
public sealed class DemoCommerceSeeder(AppDbContext db, TimeProvider clock, ILogger<DemoCommerceSeeder> logger)
{
    private const string Student = "student@courseacademy.demo";
    private const string Learner = "learner@courseacademy.demo";

    private sealed record DemoPayment(string No, int DaysAgo, string Email, int CourseId, string Method, string Masked, string Status, string? FailureReason = null);

    private static readonly DemoPayment[] Payments =
    [
        new("DEMO-SAMPLE-0001", 12, Student, 1, "bkash", "017•••••000", PaymentStatusCodes.Success),
        new("DEMO-SAMPLE-0002", 9, Learner, 3, "card", "•••• •••• •••• 4242", PaymentStatusCodes.Failed, "Card declined by issuer (simulated)"),
        new("DEMO-SAMPLE-0003", 8, Learner, 3, "nagad", "018•••••000", PaymentStatusCodes.Success),
        new("DEMO-SAMPLE-0004", 3, Learner, 2, "rocket", "019••••••00-0", PaymentStatusCodes.Cancelled),
        new("DEMO-SAMPLE-0005", 10, Student, 4, "bkash", "017•••••000", PaymentStatusCodes.Success),
        new("DEMO-SAMPLE-0006", 7, Learner, 4, "nagad", "018•••••000", PaymentStatusCodes.Success),
    ];

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.PaymentTransactions.IgnoreQueryFilters().AnyAsync(ct)) return;
        var users = await db.Users.Where(u => u.Email == Student || u.Email == Learner).ToDictionaryAsync(u => u.Email, ct);
        var courses = await db.Courses.ToDictionaryAsync(c => c.Id, ct);
        var methods = await db.PaymentMethods.ToDictionaryAsync(m => m.Code, m => m.Id, ct);
        if (users.Count < 2 || courses.Count == 0) return;

        var now = clock.GetUtcNow().UtcDateTime;
        var created = new List<(PaymentTransaction Payment, DateTime At)>();
        foreach (var p in Payments.Where(p => courses.ContainsKey(p.CourseId)))
        {
            var user = users[p.Email];
            var course = courses[p.CourseId];
            var at = now.AddDays(-p.DaysAgo);
            var payment = new PaymentTransaction
            {
                TransactionNo = p.No, Gateway = DemoPaymentGateway.GatewayName, UserId = user.Id, UserName = user.Name, UserEmail = user.Email,
                CourseId = course.Id, CourseSlug = course.Slug, CourseTitle = course.Title, PaymentMethodId = methods[p.Method], MaskedAccount = p.Masked,
                OriginalAmount = course.OriginalPrice, DiscountAmount = course.OriginalPrice - course.Price, Amount = course.Price,
                CurrencyId = LookupIds.Currency("BDT"), PaymentStatusId = LookupIds.PaymentStatus(p.Status), FailureReason = p.FailureReason,
                ExpiresAt = at.AddMinutes(30), FulfilledAt = p.Status == PaymentStatusCodes.Success ? at : null,
            };
            db.PaymentTransactions.Add(payment);
            if (p.Status == PaymentStatusCodes.Success)
            {
                db.Enrollments.Add(new Enrollment
                {
                    UserId = user.Id, CourseId = course.Id, PaymentTransaction = payment, EnrolledAt = at,
                    EnrollmentStatusId = LookupIds.EnrollmentStatus(EnrollmentStatusCodes.Active),
                });
            }
            created.Add((payment, at));
        }
        await db.SaveChangesAsync(ct);

        // The save stamps "now"; the demo records are dated in the past, like in the frontend demo.
        foreach (var (payment, at) in created)
        {
            await db.PaymentTransactions.Where(t => t.Id == payment.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedAt, at), ct);
        }
        logger.LogInformation("Added {Count} demo payments and their enrollments.", created.Count);
    }
}
