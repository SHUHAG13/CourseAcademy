using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Services;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<TokensDto> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct);
    Task<MeDto> GetMeAsync(CancellationToken ct);
}

public sealed class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokens,
    ICurrentUser currentUser,
    IScreenCatalog catalog,
    IOptions<JwtSettings> jwt,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    public const string InvalidCredentialsMessage = "Incorrect email or password.";
    public const string SessionExpiredMessage = "Your session has expired. Please log in again.";

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, ct))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        // Public sign-up always creates a student (BR-AUTH-03).
        var studentRole = await roles.GetByCodeAsync(Role.StudentCode, ct)
            ?? throw new InvalidOperationException("The student role is missing from the database.");
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            RoleId = studentRole.Id,
            Role = studentRole,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await users.AddAsync(user, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            // Two sign-ups with the same email at the same moment: the unique index stopped the second.
            throw new ConflictException("An account with this email already exists.");
        }

        logger.LogInformation("User {UserId} registered.", user.Id);
        // Sign-up keeps the user signed in like "Remember me" (BR-AUTH-04).
        return await IssueSessionAsync(user, persistent: true, ct);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), ct);
        if (user is null)
        {
            // Hash anyway so an unknown email takes as long as a wrong password.
            passwordHasher.HashPassword(new User(), request.Password);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogInformation("Failed sign-in for user {UserId}.", user.Id);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        return await IssueSessionAsync(user, request.Remember, ct);
    }

    public async Task<TokensDto> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var token = await refreshTokens.FindByHashAsync(tokens.HashRefreshToken(request.RefreshToken), ct);
        if (token is null) throw new UnauthorizedException(SessionExpiredMessage);

        if (token.RevokedAt is not null)
        {
            // A used token came back: it was probably stolen. End every session of this user.
            logger.LogWarning("Reuse of refresh token {TokenId} for user {UserId}; revoking all sessions.", token.Id, token.UserId);
            foreach (var active in await refreshTokens.ListActiveAsync(token.UserId, now, ct)) active.RevokedAt = now;
            await unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException(SessionExpiredMessage);
        }
        if (token.ExpiresAt <= now) throw new UnauthorizedException(SessionExpiredMessage);

        var (replacement, value) = NewRefreshToken(token.User, token.IsPersistent, now);
        await refreshTokens.AddAsync(replacement, ct);
        token.RevokedAt = now;
        token.ReplacedByToken = replacement;

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request used this token at the same moment and won.
            throw new UnauthorizedException(SessionExpiredMessage);
        }

        var access = tokens.CreateAccessToken(token.User);
        return new TokensDto(access.Value, access.ExpiresAt, value);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return;
        var token = await refreshTokens.FindByHashAsync(tokens.HashRefreshToken(request.RefreshToken), ct);
        if (token is null || token.RevokedAt is not null) return;
        token.RevokedAt = clock.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<MeDto> GetMeAsync(CancellationToken ct)
    {
        var user = await users.GetWithAccessAsync(currentUser.RequireUserId(), ct) ?? throw new UnauthorizedException();
        return ToMe(user, new UserAccess(user, await catalog.GetAsync(ct)));
    }

    private async Task<AuthResultDto> IssueSessionAsync(User user, bool persistent, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var expired in await refreshTokens.ListExpiredAsync(user.Id, now, ct)) refreshTokens.Remove(expired);

        var (refreshToken, value) = NewRefreshToken(user, persistent, now);
        await refreshTokens.AddAsync(refreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var access = tokens.CreateAccessToken(user);
        var withAccess = await users.GetWithAccessAsync(user.Id, ct) ?? throw new UnauthorizedException();
        return new AuthResultDto(access.Value, access.ExpiresAt, value, ToMe(withAccess, new UserAccess(withAccess, await catalog.GetAsync(ct))));
    }

    private (RefreshToken Token, string Value) NewRefreshToken(User user, bool persistent, DateTime now)
    {
        var created = tokens.CreateRefreshToken();
        var settings = jwt.Value;
        var lifetime = persistent ? TimeSpan.FromDays(settings.RefreshTokenDays) : TimeSpan.FromHours(settings.SessionRefreshTokenHours);
        return (new RefreshToken
        {
            User = user,
            UserId = user.Id,
            TokenHash = created.Hash,
            ExpiresAt = now.Add(lifetime),
            IsPersistent = persistent,
        }, created.Value);
    }

    internal static MeDto ToMe(User user, UserAccess access) => new(
        user.Id, user.Name, user.Email, user.Phone, user.Role.Code, user.Role.Name,
        access.Permissions,
        access.CourseGrants.Select(g => new CourseGrantDto(g.Key, g.Value.Order(StringComparer.Ordinal).ToList())).OrderBy(g => g.CourseId).ToList(),
        access.HasAdminPanel, access.HomeUrl, user.CreatedAt);

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
