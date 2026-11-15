using CourseAcademy.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Adds the frontend's demo accounts when they are missing. Runs only when Database:SeedDemoData is true
/// (development). Safe to run on every start-up: existing accounts are left unchanged.
/// </summary>
public sealed class DemoDataSeeder(AppDbContext db, IPasswordHasher<User> passwordHasher, ILogger<DemoDataSeeder> logger)
{
    private sealed record DemoUser(string Name, string Email, string Password, string RoleCode);

    // Same accounts and passwords as the demo login buttons in the Angular app.
    private static readonly DemoUser[] Users =
    [
        new("Academy Admin", "admin@courseacademy.demo", "Admin@123", Role.AdminCode),
        new("Demo Student", "student@courseacademy.demo", "Student@123", Role.StudentCode),
        new("Sample Learner", "learner@courseacademy.demo", "Learner@123", Role.StudentCode),
        new("Tanvir Hasan", "instructor@courseacademy.demo", "Instructor@123", Role.InstructorCode),
        new("Sabbir Ahmed", "sabbir@courseacademy.demo", "Instructor@123", Role.InstructorCode),
    ];

    public async Task SeedAsync(CancellationToken ct)
    {
        var roles = await db.Roles.ToDictionaryAsync(r => r.Code, r => r.Id, ct);
        var existing = (await db.Users.IgnoreQueryFilters().Select(u => u.Email).ToListAsync(ct)).ToHashSet();
        var added = 0;

        foreach (var demo in Users.Where(u => !existing.Contains(u.Email)))
        {
            var user = new User { Name = demo.Name, Email = demo.Email, RoleId = roles[demo.RoleCode] };
            user.PasswordHash = passwordHasher.HashPassword(user, demo.Password);
            db.Users.Add(user);
            added++;
        }

        if (added == 0) return;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Added {Count} demo account(s).", added);
    }
}
