using Cupo.Api.Data;
using Cupo.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var connection = _db.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
        await _db.Database.OpenConnectionAsync(ct);

    await using (var cmd = connection.CreateCommand())
    {
        cmd.Transaction = _db.Database.CurrentTransaction!.GetDbTransaction();
        cmd.CommandText = """SELECT "Id" FROM "Events" WHERE "Id" = @id FOR UPDATE""";
        var p = cmd.CreateParameter();
        p.ParameterName = "id";
        p.Value = eventId;
        cmd.Parameters.Add(p);

        var found = await cmd.ExecuteScalarAsync(ct);
        if (found is null or DBNull)
            throw new InvalidOperationException("NOT_FOUND");
    }

    var ev = await _db.Events.SingleAsync(e => e.Id == eventId, ct);

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
    await tx.CommitAsync(ct);

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