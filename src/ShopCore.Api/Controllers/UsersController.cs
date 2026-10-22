using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(
    IUserProfileService userProfileService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var profile = await userProfileService.GetAsync(currentUser.UserId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await userProfileService.UpdateAsync(currentUser.UserId, request, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("me/shipping")]
    [ProducesResponseType<UserShippingAddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserShippingAddressDto>> GetShipping(CancellationToken cancellationToken)
    {
        var addr = await userProfileService.GetShippingAsync(currentUser.UserId, cancellationToken);
        if (addr is null)
        {
            return NoContent();
        }

        return Ok(addr);
    }

    [HttpPut("me/shipping")]
    [ProducesResponseType<UserShippingAddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserShippingAddressDto>> UpsertShipping(UpsertUserShippingAddressRequest request, CancellationToken cancellationToken)
    {
        var addr = await userProfileService.UpsertShippingAsync(currentUser.UserId, request, cancellationToken);
        return Ok(addr);
    }
}
