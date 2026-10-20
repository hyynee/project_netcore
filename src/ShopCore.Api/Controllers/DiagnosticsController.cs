using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/diagnostics")]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableRateLimiting("api")]
public sealed class DiagnosticsController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("error")]
    public IActionResult Error()
    {
        if (!environment.IsEnvironment("Testing") && !environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException("Intentional pipeline test exception.");
    }
}
