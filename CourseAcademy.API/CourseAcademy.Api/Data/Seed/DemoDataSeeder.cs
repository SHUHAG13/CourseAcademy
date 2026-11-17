using CourseAcademy.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Adds the demo accounts when they are missing: one per built-in role. Runs only when Database:SeedDemoData is
/// true (development). Safe to run on every start-up: existing accounts are left unchanged.
/// </summary>
public sealed class DemoDataSeeder(AppDbContext db, IPasswordHasher<User> passwordHasher, ILogger<DemoDataSeeder> logger)
{
    public const string AdminEmail = "admin@courseacademy.demo";
    public const string InstructorEmail = "instructor@courseacademy.demo";
    public const string StudentEmail = "student@courseacademy.demo";

    private sealed record DemoUser(string Name, string Email, string Password, string RoleCode);

    // Same accounts and passwords as the demo login buttons in the Angular app.
    private static readonly DemoUser[] Users =
    [
        new("Academy Admin", AdminEmail, "Admin@123", Role.AdminCode),
        new("Tanvir Hasan", InstructorEmail, "Instructor@123", Role.InstructorCode),
        new("Demo Student", StudentEmail, "Student@123", Role.StudentCode),
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
