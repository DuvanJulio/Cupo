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

    [HttpPost("holds/{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        try
        {
            var hold = await _holds.ConfirmAsync(UserId, id, ct);
            return Ok(hold);
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_FOUND")
        {
            return NotFound(new { error = "Hold no existe", code = "NOT_FOUND" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_OWNER")
        {
            return StatusCode(403, new { error = "No es tuyo", code = "NOT_OWNER" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "HOLD_EXPIRED")
        {
            return StatusCode(410, new { error = "Hold vencido", code = "HOLD_EXPIRED" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "CONFLICT")
        {
            return Conflict(new { error = "Hold no se puede confirmar", code = "CONFLICT" });
        }
    }

    [HttpPost("holds/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
        try
        {
            await _holds.CancelAsync(UserId, role, id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_FOUND")
        {
            return NotFound(new { error = "Hold no existe", code = "NOT_FOUND" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_OWNER")
        {
            return StatusCode(403, new { error = "No es tuyo", code = "NOT_OWNER" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "CONFLICT")
        {
            return Conflict(new { error = "Hold no se puede cancelar", code = "CONFLICT" });
        }
    }
}


