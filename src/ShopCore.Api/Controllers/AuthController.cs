using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("auth-register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!new EmailAddressAttribute().IsValid(request.Email) ||
            request.Password is null || request.Password.Length < 12)
        {
            return BadRequest(new ProblemDetails { Title = "The request is invalid.", Status = 400 });
        }

        var response = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("auth-login")]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("auth-refresh")]
    public Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("auth-changepw")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.NewPassword is null || request.NewPassword.Length < 12)
        {
            return BadRequest(new ProblemDetails { Title = "The request is invalid.", Status = 400 });
        }

        await authService.ChangePasswordAsync(currentUser.UserId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("auth-logoutall")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await authService.LogoutAllAsync(currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
