using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Tests.Services;

public class AuthServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Register_creates_a_student_with_a_normalised_email_and_signs_in()
    {
        var kit = new IdentityTestKit();

        var result = await kit.Auth.RegisterAsync(new RegisterRequest("  New Learner ", "  New@Example.COM ", "Secret123", "", true), Ct);

        Assert.Equal("new@example.com", result.Me.Email);
        Assert.Equal("New Learner", result.Me.Name);
        Assert.Null(result.Me.Phone);
        Assert.Equal("user", result.Me.RoleCode);
        Assert.Equal("/dashboard", result.Me.HomeUrl);
        Assert.False(result.Me.HasAdminPanel);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);

        var stored = await kit.Db.Users.SingleAsync(u => u.Email == "new@example.com");
        Assert.NotEqual("Secret123", stored.PasswordHash);
        var token = await kit.Db.RefreshTokens.SingleAsync(t => t.UserId == stored.Id);
        Assert.True(token.IsPersistent);
        Assert.NotEqual(result.RefreshToken, token.TokenHash);
    }

    [Fact]
    public async Task Register_rejects_an_existing_email_in_any_case()
    {
        var kit = new IdentityTestKit();

        var error = await IdentityTestKit.Throws<ConflictException>(() =>
            kit.Auth.RegisterAsync(new RegisterRequest("X", "STUDENT@courseacademy.demo", "Secret123", null, true), Ct));

        Assert.Equal("An account with this email already exists.", error.Message);
    }

    [Fact]
    public async Task Login_succeeds_with_trimmed_case_insensitive_email()
    {
        var kit = new IdentityTestKit();

        var result = await kit.Auth.LoginAsync(new LoginRequest(" Admin@CourseAcademy.demo ", "Admin@123", Remember: false), Ct);

        Assert.Equal("admin", result.Me.RoleCode);
        Assert.Equal("/admin", result.Me.HomeUrl);
        Assert.Equal(27, result.Me.Permissions.Count);
        Assert.False((await kit.Db.RefreshTokens.SingleAsync()).IsPersistent);
    }

    [Theory]
    [InlineData("admin@courseacademy.demo", "wrong")]
    [InlineData("nobody@courseacademy.demo", "Admin@123")]
    public async Task Login_gives_the_same_error_for_unknown_email_and_wrong_password(string email, string password)
    {
        var kit = new IdentityTestKit();

        var error = await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.LoginAsync(new LoginRequest(email, password), Ct));

        Assert.Equal(AuthService.InvalidCredentialsMessage, error.Message);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_keeps_persistence()
    {
        var kit = new IdentityTestKit();
        var login = await kit.Auth.LoginAsync(new LoginRequest(IdentityTestKit.Student, "Student@123", Remember: true), Ct);

        var refreshed = await kit.Auth.RefreshAsync(new RefreshRequest(login.RefreshToken), Ct);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        var tokens = await kit.Db.RefreshTokens.OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Equal(tokens[1].Id, tokens[0].ReplacedByTokenId);
        Assert.True(tokens[1].IsPersistent);
        Assert.Null(tokens[1].RevokedAt);
    }

    [Fact]
    public async Task Reusing_a_used_refresh_token_ends_every_session()
    {
        var kit = new IdentityTestKit();
        var first = await kit.Auth.LoginAsync(new LoginRequest(IdentityTestKit.Student, "Student@123"), Ct);
        var otherDevice = await kit.Auth.LoginAsync(new LoginRequest(IdentityTestKit.Student, "Student@123"), Ct);
        var rotated = await kit.Auth.RefreshAsync(new RefreshRequest(first.RefreshToken), Ct);

        await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.RefreshAsync(new RefreshRequest(first.RefreshToken), Ct));

        await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.RefreshAsync(new RefreshRequest(rotated.RefreshToken), Ct));
        await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.RefreshAsync(new RefreshRequest(otherDevice.RefreshToken), Ct));
    }

    [Fact]
    public async Task Session_tokens_expire_after_a_day_and_remembered_ones_after_30_days()
    {
        var kit = new IdentityTestKit();
        var session = await kit.Auth.LoginAsync(new LoginRequest(IdentityTestKit.Student, "Student@123", Remember: false), Ct);
        var remembered = await kit.Auth.LoginAsync(new LoginRequest(IdentityTestKit.Learner, "Learner@123", Remember: true), Ct);

        kit.Clock.Advance(TimeSpan.FromHours(25));

        await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.RefreshAsync(new RefreshRequest(session.RefreshToken), Ct));
        Assert.NotNull(await kit.Auth.RefreshAsync(new RefreshRequest(remembered.RefreshToken), Ct));
    }

    [Fact]
    public async Task Unknown_refresh_token_is_rejected()
    {
        var kit = new IdentityTestKit();

        var error = await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.RefreshAsync(new RefreshRequest("made-up"), Ct));

        Assert.Equal(AuthService.SessionExpiredMessage, error.Message);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var kit = new IdentityTestKit();
        var login = await kit.Auth.LoginAsync(new LoginRequest(IdentityTestKit.Student, "Student@123"), Ct);

        await kit.Auth.LogoutAsync(new LogoutRequest(login.RefreshToken), Ct);
        await kit.Auth.LogoutAsync(new LogoutRequest("unknown"), Ct); // never fails

        await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.RefreshAsync(new RefreshRequest(login.RefreshToken), Ct));
    }

    [Fact]
    public async Task Me_requires_a_signed_in_user()
    {
        var kit = new IdentityTestKit();

        await IdentityTestKit.Throws<UnauthorizedException>(() => kit.Auth.GetMeAsync(Ct));

        var me = await kit.As(IdentityTestKit.Instructor).Auth.GetMeAsync(Ct);
        Assert.Equal("instructor", me.RoleCode);
        Assert.Equal(["dashboard.view", "learn.view", "checkout.view", "community.view", "community.post"], me.Permissions);
    }
}
