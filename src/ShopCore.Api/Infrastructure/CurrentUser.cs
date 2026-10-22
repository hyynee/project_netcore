using System.Security.Claims;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Infrastructure;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var raw = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("Missing or invalid user claim.");
        }
    }

    public Guid? UserIdIfPresent
    {
        get
        {
            var raw = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var userId) ? userId : null;
        }
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return accessor.HttpContext?.User.IsInRole(role) == true;
    }
}
