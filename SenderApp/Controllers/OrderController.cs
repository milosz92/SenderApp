using Messages;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace SenderApp.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IMessageSession _messageSession;

    public OrderController(IMessageSession messageSession)
    {
        _messageSession = messageSession;
    }

    [HttpGet]
    public async Task<IActionResult> PlaceOrder()
    {
        var orderId = Guid.NewGuid();
        
        var orderPlaced = new OrderPlaced
        {
            OrderId = orderId,
            OrderDetails = "Sample Order",
            PlacedAt = DateTime.UtcNow
        };

        await _messageSession.Publish(orderPlaced);

        return Accepted(new { OrderId = orderId, Status = "Accepted" });
    }

    /// <summary>
    /// Publish OrderPlaced event with different scenarios for testing Polly in ClientB
    /// </summary>
    /// <param name="exceptionType">
    /// - "retry": Triggers RetryableException in ClientB (Polly will retry 3 times)
    /// - "circuit-breaker": Triggers CircuitBreakerException in ClientB (opens circuit after threshold)
    /// - null or empty: Success scenario
    /// </param>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessOrder([FromQuery] string? exceptionType = null)
    {
        var orderId = Guid.NewGuid();
        
        var orderPlaced = new OrderPlaced
        {
            OrderId = orderId,
            OrderDetails = $"Order with exception type: {exceptionType ?? "none"}",
            PlacedAt = DateTime.UtcNow,
            ExceptionType = exceptionType
        };

        await _messageSession.Publish(orderPlaced);

        return Accepted(new 
        { 
            OrderId = orderId, 
            Status = "Event Published",
            EventType = "OrderPlaced",
            ExceptionType = exceptionType ?? "none (success)",
            Subscribers = "ClientA and ClientB will both receive this event",
            Description = exceptionType switch
            {
                "retry" => "ClientB will trigger RetryableException - Polly will retry 3 times",
                "circuit-breaker" => "ClientB will trigger CircuitBreakerException - Circuit breaker will open after threshold",
                _ => "Both clients will process successfully"
            }
        });
    }
}
