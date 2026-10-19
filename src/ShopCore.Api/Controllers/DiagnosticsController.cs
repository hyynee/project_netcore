using Microsoft.AspNetCore.Mvc;

namespace ShopCore.Api.Controllers;

[ApiController]
[Route("api/v1/diagnostics")]
[ApiExplorerSettings(IgnoreApi = true)]
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
