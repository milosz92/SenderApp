using Messages;
using NServiceBus;
using NServiceBus.Logging;

namespace ClientA.Handlers;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    static readonly ILog log = LogManager.GetLogger<OrderPlacedHandler>();

    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        //// Log delivery attempt information
        //if (context.MessageHeaders.TryGetValue(Headers.DelayedRetries, out var delayedRetries))
        //{
        //    log.Info($"Delayed retry attempt: {delayedRetries}");
        //}
        //if (context.MessageHeaders.TryGetValue(Headers.ImmediateRetries, out var immediateRetries))
        //{
        //    log.Info($"Immediate retry attempt: {immediateRetries}");
        //}

        log.Info($"Received OrderPlaced event - OrderId: {message.OrderId}, Details: {message.OrderDetails}, PlacedAt: {message.PlacedAt}");
        
        Console.WriteLine("===========================================");
        Console.WriteLine($"Order Received!");
        Console.WriteLine($"Order ID: {message.OrderId}");
        Console.WriteLine($"Details: {message.OrderDetails}");
        Console.WriteLine($"Placed At: {message.PlacedAt}");
        Console.WriteLine("===========================================");
        
        return Task.CompletedTask;
    }
}
