using System.Security.Claims;
using Cupo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cupo.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class HoldsController : ControllerBase
{
    private readonly HoldService _holds;
    public HoldsController(HoldService holds) => _holds = holds;

    public record CreateHoldRequest(Guid EventId);

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("holds")]
    public async Task<IActionResult> Create(CreateHoldRequest req, CancellationToken ct)
    {
        try
        {
            var hold = await _holds.CreateAsync(UserId, req.EventId, ct);
            return Created($"/api/holds/{hold.Id}", hold);
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_FOUND")
        {
            return NotFound(new { error = "Evento no existe", code = "NOT_FOUND" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "SOLD_OUT")
        {
            return Conflict(new { error = "Sin cupos", code = "SOLD_OUT" });
        }
    }

    [HttpGet("me/holds")]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await _holds.ListMineAsync(UserId, ct));
}


