using System.ComponentModel.DataAnnotations;

namespace ShopCore.Application.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(12)] string Password);
public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
public sealed record RefreshRequest(
    [Required] string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(12)] string NewPassword);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    Guid UserId { get; }
}
