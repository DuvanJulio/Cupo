namespace Cupo.Api.Domain;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTimeOffset StartsAt { get; set; }
    public int Capacity { get; set; }
    public int HeldCount { get; set; }
    public int ConfirmedCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

}