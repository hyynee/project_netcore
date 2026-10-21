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
}
