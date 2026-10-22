using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ProductsController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CatalogPage<ProductSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CatalogPage<ProductSummary>>> List(
        [FromQuery] ProductQueryRequest query,
        CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetProductsAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ProductDetails>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetPublicProductAsync(id, cancellationToken));
    }
}
