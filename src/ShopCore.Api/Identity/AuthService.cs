using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShopCore.Api.Configuration;
using ShopCore.Api.Exceptions;
using ShopCore.Application.Contracts;
using ShopCore.Domain.Entities;
using ShopCore.Infrastructure.Persistence;

namespace ShopCore.Api.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ShopCoreDbContext db,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Where(x => !x.Code.StartsWith("Duplicate", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Code, x => new[] { x.Description });
            throw new ValidationException(
                "Registration failed.",
                errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, ShopRoles.Customer);
        if (!roleResult.Succeeded)
        {
            try
            {
                await userManager.DeleteAsync(user);
            }
            catch
            {
            }

            throw new InvalidOperationException("Could not assign the default customer role.");
        }
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        var valid = user is not null &&
            (await signInManager.CheckPasswordSignInAsync(user, request.Password, true)).Succeeded;
        if (!valid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await IssueTokensAsync(user!, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = Hash(request.RefreshToken);
        var token = await db.RefreshTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null || token.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (token.RevokedAt is not null)
        {
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        token.RevokedAt = now;
        var response = await IssueTokensAsync(token.User, cancellationToken, token.FamilyId, saveChanges: false);
        token.ReplacedByTokenHash = Hash(response.RefreshToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }
        return response;
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var token = await db.RefreshTokens.SingleOrDefaultAsync(
            x => x.TokenHash == Hash(request.RefreshToken), cancellationToken);
        if (token is null) return;

        await RevokeFamilyAsync(token.FamilyId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException("Password change failed.",
                result.Errors.ToDictionary(x => x.Code, x => new[] { x.Description }));
        }

        await RevokeAllAsync(userId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await RevokeAllAsync(userId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        Guid? familyId = null,
        bool saveChanges = true)
    {
        var options = jwtOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(options.AccessTokenMinutes);
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(options.Issuer, options.Audience, claims, now, expires, credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            User = user,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = Hash(refreshToken),
            ExpiresAt = now.AddDays(options.RefreshTokenDays)
        });
        if (saveChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        return new AuthResponse(accessToken, refreshToken, expires);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken)
    {
        await db.RefreshTokens.Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
