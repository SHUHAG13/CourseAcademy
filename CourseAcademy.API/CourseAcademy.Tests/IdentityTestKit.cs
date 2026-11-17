using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CourseAcademy.Tests;

/// <summary>Real services and repositories over an in-memory database, with a settable signed-in user.</summary>
internal sealed class IdentityTestKit
{
    public AppDbContext Db { get; }
    public FakeTimeProvider Clock { get; }
    public TestCurrentUser CurrentUser { get; }
    public AuthService Auth { get; }
    public AccessService Access { get; }
    public ScreenCatalog Catalog { get; }
    public UserRepository Users { get; }

    public static readonly JwtSettings Jwt = new()
    {
        Issuer = "test", Audience = "test", SigningKey = new string('k', 64),
        AccessTokenMinutes = 15, RefreshTokenDays = 30, SessionRefreshTokenHours = 24,
    };

    public IdentityTestKit()
    {
        (Db, Clock) = TestDb.Create();
        var hasher = new PasswordHasher<User>();
        Users = new UserRepository(Db);
        var roles = new RoleRepository(Db);
        Catalog = new ScreenCatalog(new ScreenRepository(Db), new MemoryCache(new MemoryCacheOptions()));
        CurrentUser = new TestCurrentUser(Users, Catalog);
        var unitOfWork = new UnitOfWork(Db);
        var options = Microsoft.Extensions.Options.Options.Create(Jwt);

        Auth = new AuthService(Users, roles, new RefreshTokenRepository(Db), unitOfWork, hasher,
            new TokenService(options, Clock), CurrentUser, Catalog, options, Clock, NullLogger<AuthService>.Instance);
        Access = new AccessService(Users, roles, new Repository<RoleScreen>(Db), new Repository<UserScreen>(Db),
            new Repository<CourseGrant>(Db), new Repository<CourseGrantPermission>(Db), unitOfWork, Catalog, CurrentUser);

        new DemoDataSeeder(Db, hasher, NullLogger<DemoDataSeeder>.Instance).SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
        // A second student for tests that need two learners; the demo seed has one account per role.
        var learner = new User { Name = "Sample Learner", Email = Learner, RoleId = Db.Roles.Single(r => r.Code == Role.StudentCode).Id };
        learner.PasswordHash = hasher.HashPassword(learner, "Learner@123");
        Db.Users.Add(learner);
        Db.SaveChanges();
        Db.ChangeTracker.Clear();
    }

    public int UserId(string email) => Db.Users.Single(u => u.Email == email).Id;

    public const string Admin = "admin@courseacademy.demo";
    public const string Student = "student@courseacademy.demo";
    public const string Learner = "learner@courseacademy.demo";
    public const string Instructor = "instructor@courseacademy.demo";

    /// <summary>Signs in as the given demo account for the next service calls.</summary>
    public IdentityTestKit As(string email)
    {
        CurrentUser.Set(UserId(email));
        Db.ChangeTracker.Clear();
        return this;
    }

    public static async Task<T> Throws<T>(Func<Task> action) where T : AppException => await Assert.ThrowsAsync<T>(action);
}

/// <summary>ICurrentUser for tests: same rules as the real one, with the user id set directly.</summary>
internal sealed class TestCurrentUser(IUserRepository users, IScreenCatalog catalog) : ICurrentUser
{
    public int? UserId { get; private set; }

    public void Set(int? id) => UserId = id;

    public int RequireUserId() => UserId ?? throw new UnauthorizedException();

    public async Task<UserAccess> GetAccessAsync(CancellationToken ct)
    {
        var user = await users.GetWithAccessAsync(RequireUserId(), ct) ?? throw new UnauthorizedException();
        return new UserAccess(user, await catalog.GetAsync(ct));
    }

    public async Task<UserAccess?> TryGetAccessAsync(CancellationToken ct)
    {
        if (UserId is not { } id) return null;
        var user = await users.GetWithAccessAsync(id, ct);
        return user is null ? null : new UserAccess(user, await catalog.GetAsync(ct));
    }

    public async Task EnsureCanAsync(string permission, int? courseId, CancellationToken ct)
    {
        if (!(await GetAccessAsync(ct)).Can(permission, courseId)) throw new ForbiddenException();
    }
}

internal static class SeedIds
{
    public const int StudentRole = AccessSeed.StudentRoleId;
}
