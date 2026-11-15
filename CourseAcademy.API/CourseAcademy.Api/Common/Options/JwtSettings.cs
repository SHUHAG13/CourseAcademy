using System.ComponentModel.DataAnnotations;

namespace CourseAcademy.Api.Common.Options;

/// <summary>Section "Jwt" in configuration. The signing key is a secret: set it with user secrets or an environment variable.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required, MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters. Set it with user secrets or the Jwt__SigningKey environment variable.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Lifetime of a "Remember me" session.</summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;

    /// <summary>Lifetime of a session without "Remember me".</summary>
    [Range(1, 720)]
    public int SessionRefreshTokenHours { get; set; } = 24;
}

/// <summary>Section "Auth" in configuration.</summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";
    public const string RateLimitPolicy = "auth";

    /// <summary>Sign-in, sign-up and refresh attempts allowed per client address per minute.</summary>
    [Range(1, 10_000)]
    public int RequestsPerMinute { get; set; } = 20;
}
