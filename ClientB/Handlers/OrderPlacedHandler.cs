using Messages;
using NServiceBus;
using Polly.CircuitBreaker;
using ClientB.Services;

namespace ClientB.Handlers;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    private readonly PollyPolicies _pollyPolicies;
    private static int _retryAttemptCounter = 0;
    private static int _circuitBreakerAttemptCounter = 0;

    public OrderPlacedHandler(PollyPolicies pollyPolicies)
    {
        _pollyPolicies = pollyPolicies;
    }

    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine($"[ClientB] Received OrderPlaced Event");
        Console.WriteLine($"[ClientB] OrderId: {message.OrderId}");
        Console.WriteLine($"[ClientB] OrderDetails: {message.OrderDetails}");
        Console.WriteLine($"[ClientB] PlacedAt: {message.PlacedAt}");
        Console.WriteLine($"[ClientB] ExceptionType: {message.ExceptionType ?? "none (success)"}");
        Console.WriteLine("===========================================");

        try
        {
            if (message.ExceptionType == "retry")
            {
                // Use Retry Policy
                Console.WriteLine("[ClientB] Applying RETRY POLICY with Polly...");
                var result = await _pollyPolicies.RetryPipeline.ExecuteAsync(
                    async token => await ProcessWithRetryableException(message),
                    context.CancellationToken);
                
                Console.WriteLine($"[ClientB] ? SUCCESS: {result}");
            }
            else if (message.ExceptionType == "circuit-breaker")
            {
                // Use Circuit Breaker Policy
                Console.WriteLine("[ClientB] Applying CIRCUIT BREAKER POLICY with Polly...");
                
                try
                {
                    var result = await _pollyPolicies.CircuitBreakerPipeline.ExecuteAsync(
                        async token => await ProcessWithCircuitBreakerException(message),
                        context.CancellationToken);
                    
                    Console.WriteLine($"[ClientB] ? SUCCESS: {result}");
                }
                catch (BrokenCircuitException ex)
                {
                    Console.WriteLine($"[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately");
                    Console.WriteLine($"[ClientB] Exception: {ex.Message}");
                    throw; // Re-throw to fail the message
                }
            }
            else
            {
                // Normal processing without exceptions
                var result = await ProcessSuccessfully(message);
                Console.WriteLine($"[ClientB] ? SUCCESS: {result}");
            }
        }
        catch (RetryableException ex)
        {
            Console.WriteLine($"[ClientB] ? FAILED after all retry attempts");
            Console.WriteLine($"[ClientB] Final Exception: {ex.Message}");
            throw; // Re-throw to send to error queue
        }
        catch (CircuitBreakerException ex)
        {
            Console.WriteLine($"[ClientB] ? CIRCUIT BREAKER EXCEPTION - Circuit may open");
            Console.WriteLine($"[ClientB] Exception: {ex.Message}");
            throw; // Re-throw to send to error queue
        }

        Console.WriteLine("===========================================");
    }

    private async Task<string> ProcessWithRetryableException(OrderPlaced message)
    {
        _retryAttemptCounter++;
        Console.WriteLine($"[ProcessWithRetryableException] Execution attempt #{_retryAttemptCounter}");
        
        // Simulate transient failure - always fail for demo purposes
        await Task.Delay(100); // Simulate some work
        
        throw new RetryableException($"Simulated transient error for OrderId: {message.OrderId}");
    }

    private async Task<string> ProcessWithCircuitBreakerException(OrderPlaced message)
    {
        _circuitBreakerAttemptCounter++;
        Console.WriteLine($"[ProcessWithCircuitBreakerException] Execution attempt #{_circuitBreakerAttemptCounter}");
        
        // Simulate critical failure - always fail for demo purposes
        await Task.Delay(100); // Simulate some work
        
        throw new CircuitBreakerException($"Simulated critical error for OrderId: {message.OrderId}");
    }

    private async Task<string> ProcessSuccessfully(OrderPlaced message)
    {
        Console.WriteLine($"[ProcessSuccessfully] Processing order {message.OrderId}...");
        
        // Simulate successful processing
        await Task.Delay(500);
        
        return $"Order {message.OrderId} processed successfully by ClientB!";
    }
}
