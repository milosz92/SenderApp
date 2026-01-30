using Messages;

namespace ClientA.Handlers;

public class OrderPlacedHandler
{
    public Task<bool> HandleAsync(OrderPlaced message)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine($"Order Received!");
        Console.WriteLine($"Order ID: {message.OrderId}");
        Console.WriteLine($"Details: {message.OrderDetails}");
        Console.WriteLine($"Placed At: {message.PlacedAt}");
        Console.WriteLine("===========================================");
        
        return Task.FromResult(true);
    }
}
