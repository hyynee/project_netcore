using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopCore.Application.Contracts;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Policy = ShopPolicies.AdminOnly)]
[EnableRateLimiting("admin")]
public sealed class AdminProductsController(IAdminCatalogService adminCatalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CatalogPage<AdminProductSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CatalogPage<AdminProductSummary>>> List(
        [FromQuery] AdminProductQueryRequest query,
        CancellationToken cancellationToken)
    {
        return Ok(await adminCatalogService.GetProductsAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminProductDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDetails>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await adminCatalogService.GetProductAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<AdminProductDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminProductDetails>> Create(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await adminCatalogService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AdminProductDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminProductDetails>> Update(
        Guid id,
        ProductUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await adminCatalogService.UpdateProductAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await adminCatalogService.DeactivateProductAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/inventory-adjustments")]
    [ProducesResponseType<InventoryAdjustmentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryAdjustmentResult>> AdjustInventory(
        Guid id,
        InventoryAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await adminCatalogService.AdjustInventoryAsync(id, request, cancellationToken));
    }
}
