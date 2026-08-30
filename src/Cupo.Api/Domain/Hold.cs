namespace Cupo.Api.Domain;

public class Hold
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public HoldStatus Status { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}