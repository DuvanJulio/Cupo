using System.Diagnostics.Tracing;
using Cupo.Api.Data;
using Cupo.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cupo.Api.Services;

public class EventService
{
    private readonly AppDbContext _db;
    public EventService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EventListItem>> ListAsync(CancellationToken ct)
    {
        return await _db.Events
            .OrderBy(e => e.StartsAt)
            .Select(e => new EventListItem(
                e.Id,
                e.Title,
                e.StartsAt,
                e.Capacity,
                e.Capacity - e.HeldCount - e.ConfirmedCount))
            .ToListAsync(ct);
    }

    public async Task<EventListItem?> GetAsync(Guid id, CancellationToken ct)
    {
        return await _db.Events
            .Where(e => e.Id == id)
            .Select(e => new EventListItem(
                e.Id,
                e.Title,
                e.StartsAt,
                e.Capacity,
                e.Capacity - e.HeldCount - e.ConfirmedCount))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<EventListItem> CreateAsync(string title, DateTimeOffset startsAt, int capacity, CancellationToken ct)
    {
        if (capacity < 1)
            throw new InvalidOperationException("INVALID_CAPACITY");

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            StartsAt = startsAt,
            Capacity = capacity,
            HeldCount = 0,
            ConfirmedCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync(ct);
        return new EventListItem(ev.Id, ev.Title, ev.StartsAt, ev.Capacity, ev.Capacity);
    }
}

public record EventListItem(Guid Id, string Title, DateTimeOffset StartsAt, int Capacity, int Available);
