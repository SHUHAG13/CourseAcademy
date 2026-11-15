using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CourseAcademy.Tests.Services;

public class TokenServiceTests
{
    private static TokenService Create(FakeTimeProvider clock) => new(Options.Create(IdentityTestKit.Jwt), clock);

    [Fact]
    public async Task Access_token_is_signed_and_carries_only_the_user_id()
    {
        var clock = new FakeTimeProvider(TestDb.Start);
        var token = Create(clock).CreateAccessToken(new User { Id = 42 });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "test",
            ValidAudience = "test",
            IssuerSigningKey = TokenService.SigningKey(IdentityTestKit.Jwt),
            ValidateLifetime = false,
        });

        Assert.True(result.IsValid);
        Assert.Equal("42", result.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal(TestDb.Start.UtcDateTime.AddMinutes(15), token.ExpiresAt);
    }

    [Fact]
    public void Refresh_tokens_are_random_and_only_the_hash_is_kept()
    {
        var service = Create(new FakeTimeProvider());

        var a = service.CreateRefreshToken();
        var b = service.CreateRefreshToken();

        Assert.NotEqual(a.Value, b.Value);
        Assert.Equal(64, a.Hash.Length);
        Assert.Equal(a.Hash, service.HashRefreshToken(a.Value));
        Assert.DoesNotContain(a.Value, a.Hash);
    }
}
