using Cupo.Api.Data;
using Cupo.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cupo.Api.Services;

public class HoldService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public HoldService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<HoldResponse> CreateAsync(Guid userId, Guid eventId, CancellationToken ct)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.Id == eventId, ct)
            ?? throw new InvalidOperationException("NOT_FOUND");

        var available = ev.Capacity - ev.HeldCount - ev.ConfirmedCount;
        if (available < 1)
            throw new InvalidOperationException("SOLD_OUT");

        var minutes = _config.GetValue<int>("Holds:DurationMinutes");
        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = HoldStatus.Pending,
            Quantity = 1,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Holds.Add(hold);
        ev.HeldCount += 1;
        await _db.SaveChangesAsync(ct);

        return ToResponse(hold);
    }

    public async Task<IReadOnlyList<HoldResponse>> ListMineAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Holds
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new HoldResponse(
                h.Id, h.EventId, h.Status.ToString(), h.ExpiresAt,
                (int)Math.Max(0, (h.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)))
            .ToListAsync(ct);
    }

    private static HoldResponse ToResponse(Hold h) => new(
        h.Id, h.EventId, h.Status.ToString(), h.ExpiresAt,
        (int)Math.Max(0, (h.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds));
}

public record HoldResponse(Guid Id, Guid EventId, string Status, DateTimeOffset ExpiresAt, int SecondsLeft);