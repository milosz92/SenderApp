using Messages;
using Messages.RabbitMQ;
using Microsoft.AspNetCore.Mvc;

namespace SenderApp.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly RabbitMQPublisher _publisher;

    public OrderController(RabbitMQPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    [HttpGet]
    public IActionResult PlaceOrder()
    {
        var orderId = Guid.NewGuid();
        
        var orderPlaced = new OrderPlaced
        {
            OrderId = orderId,
            OrderDetails = "Sample Order",
            PlacedAt = DateTime.UtcNow
        };

        _publisher.Publish(orderPlaced, "order.placed");

        return Accepted(new { OrderId = orderId, Status = "Accepted" });
    }

    /// <summary>
    /// Publish OrderPlaced event with different scenarios for testing Polly and Inbox Pattern in ClientB
    /// </summary>
    /// <param name="orderId">Optional OrderId for testing (will generate new if not provided)</param>
    /// <param name="exceptionType">
    /// - "retry": Triggers RetryableException in ClientB (Polly will retry 3 times)
    /// - "circuit-breaker": Triggers CircuitBreakerException in ClientB (opens circuit after threshold)
    /// - "fatal": Triggers FatalException in ClientB (fails immediately, no resilience)
    /// - null or empty: Success scenario
    /// </param>
    [HttpPost("process")]
    public IActionResult ProcessOrder([FromQuery] Guid? orderId = null, [FromQuery] string? exceptionType = null)
    {
        var actualOrderId = orderId ?? Guid.NewGuid();
        
        var orderPlaced = new OrderPlaced
        {
            OrderId = actualOrderId,
            OrderDetails = $"Order with exception type: {exceptionType ?? "none"}",
            PlacedAt = DateTime.UtcNow,
            ExceptionType = exceptionType
        };

        _publisher.Publish(orderPlaced, "order.placed");

        return Accepted(new 
        { 
            OrderId = actualOrderId,
            OrderIdSource = orderId.HasValue ? "Provided (for testing)" : "Generated",
            Status = "Event Published",
            EventType = "OrderPlaced",
            ExceptionType = exceptionType ?? "none (success)",
            Subscribers = "ClientA and ClientB will both receive this event",
            Description = exceptionType switch
            {
                "retry" => "ClientB will trigger RetryableException - Polly will retry 3 times",
                "circuit-breaker" => "ClientB will trigger CircuitBreakerException - Circuit breaker will open after threshold",
                "fatal" => "ClientB will trigger FatalException - Fails immediately without retry or circuit breaker",
                _ => "Both clients will process successfully"
            }
        });
    }
}
