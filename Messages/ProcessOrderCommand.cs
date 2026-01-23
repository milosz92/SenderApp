namespace Messages;

public class ProcessOrderCommand
{
    public Guid OrderId { get; set; }
    public string? OrderDetails { get; set; }
    public string? ExceptionType { get; set; } // "retry", "circuit-breaker", or null for success
}
