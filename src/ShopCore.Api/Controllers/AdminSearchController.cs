using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/admin/search")]
[Authorize(Policy = ShopPolicies.AdminOnly)]
[EnableRateLimiting("admin")]
public sealed class AdminSearchController(IAdminCatalogService adminCatalogService) : ControllerBase
{
    [HttpPost("reindex")]
    [ProducesResponseType<SearchReindexResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SearchReindexResult>> Reindex(CancellationToken cancellationToken)
    {
        return Ok(await adminCatalogService.ReindexProductSearchAsync(cancellationToken));
    }
}
