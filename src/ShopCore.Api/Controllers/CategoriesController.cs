using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class CategoriesController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CatalogPage<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CatalogPage<CategoryResponse>>> List(
        [FromQuery] CategoryQueryRequest query,
        CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetActiveCategoriesAsync(query, cancellationToken));
    }
}
