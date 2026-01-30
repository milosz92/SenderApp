namespace Messages;

public class OrderPlaced
{
    public Guid OrderId { get; set; }
    public string? OrderDetails { get; set; }
    public DateTime PlacedAt { get; set; }
    public string? ExceptionType { get; set; } // "retry", "circuit-breaker", or null for success
}
