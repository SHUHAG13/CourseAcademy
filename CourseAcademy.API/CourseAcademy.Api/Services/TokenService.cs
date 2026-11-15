using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CourseAcademy.Api.Services;

public sealed record AccessToken(string Value, DateTime ExpiresAt);

/// <summary>A new refresh token: <see cref="Value"/> goes to the client once, only <see cref="Hash"/> is stored.</summary>
public sealed record NewRefreshToken(string Value, string Hash);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    NewRefreshToken CreateRefreshToken();
    string HashRefreshToken(string value);
}

public sealed class TokenService(IOptions<JwtSettings> options, TimeProvider clock) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            // Only the identity goes in the token. Permissions are read from the database on every request.
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(SigningKey(_settings), SecurityAlgorithms.HmacSha256),
        };
        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public NewRefreshToken CreateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return new NewRefreshToken(value, HashRefreshToken(value));
    }

    public string HashRefreshToken(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static SymmetricSecurityKey SigningKey(JwtSettings settings) => new(Encoding.UTF8.GetBytes(settings.SigningKey));
}
