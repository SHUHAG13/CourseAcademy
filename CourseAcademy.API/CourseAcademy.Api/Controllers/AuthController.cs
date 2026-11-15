using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CourseAcademy.Api.Controllers;

/// <summary>Sign-up, sign-in, token refresh, sign-out and the current user.</summary>
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthSettings.RateLimitPolicy)]
    [ProducesResponseType<ApiResponse<AuthResultDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        Created(await auth.RegisterAsync(request, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthSettings.RateLimitPolicy)]
    [ProducesResponseType<ApiResponse<AuthResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => Success(await auth.LoginAsync(request, ct));

    /// <summary>Exchanges a refresh token for new tokens. The old refresh token stops working.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthSettings.RateLimitPolicy)]
    [ProducesResponseType<ApiResponse<TokensDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct) => Success(await auth.RefreshAsync(request, ct));

    /// <summary>Ends the session of the given refresh token. Always succeeds, so it also works with an expired access token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return Success();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<ApiResponse<MeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct) => Success(await auth.GetMeAsync(ct));
}
