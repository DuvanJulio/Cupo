

using Cupo.Api.Domain;
using Cupo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cupo.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly EventService _events;
    public EventsController(EventService events) => _events = events;

    public record CreateEventRequest(string Title, DateTimeOffset StartsAt, int Capacity);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) 
        => Ok(await _events.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var ev = await _events.GetAsync(id, ct);
        return ev is null? NotFound() : Ok(ev);
    } 

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateEventRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Title requerido"});

        try
        {
            var ev = await _events.CreateAsync(req.Title, req.StartsAt, req.Capacity, ct);
            return Created($"/api/events/{ev.Id}", ev);
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_CAPACITY")
        {
            return BadRequest(new { error = "capacity debe ser >= 1", code = "INVALID_CAPACITY" });
            
        }
    }
}