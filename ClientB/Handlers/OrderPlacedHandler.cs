using Messages;
using ClientB.Services;
using Polly.CircuitBreaker;

namespace ClientB.Handlers;

public class OrderPlacedHandler
{
    private readonly PollyPolicies _pollyPolicies;
    private readonly InboxService _inboxService;
    private int _retryAttemptCounter = 0;
    private int _circuitBreakerAttemptCounter = 0;

    public OrderPlacedHandler(PollyPolicies pollyPolicies, InboxService inboxService)
    {
        _pollyPolicies = pollyPolicies ?? throw new ArgumentNullException(nameof(pollyPolicies));
        _inboxService = inboxService ?? throw new ArgumentNullException(nameof(inboxService));
    }

    public async Task<bool> HandleAsync(OrderPlaced message)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine($"[ClientB] Received OrderPlaced Event");
        Console.WriteLine($"[ClientB] OrderId: {message.OrderId}");
        Console.WriteLine($"[ClientB] OrderDetails: {message.OrderDetails}");
        Console.WriteLine($"[ClientB] PlacedAt: {message.PlacedAt}");
        Console.WriteLine($"[ClientB] ExceptionType: {message.ExceptionType ?? "none (success)"}");

        // INBOX PATTERN: Check for duplicates using OrderId
        if (await _inboxService.IsOrderProcessedAsync(message.OrderId))
        {
            Console.WriteLine($"[ClientB] ? DUPLICATE DETECTED - Order {message.OrderId} already processed");
            Console.WriteLine($"[ClientB] ? Returning success without reprocessing (idempotency)");
            Console.WriteLine("===========================================");
            return true;
        }

        Console.WriteLine("===========================================");

        try
        {
            if (message.ExceptionType == "retry")
            {
                Console.WriteLine("[ClientB] Applying RETRY POLICY with Polly...");
                var result = await _pollyPolicies.RetryPipeline.ExecuteAsync<string>(
                    async (context) =>
                    {
                        _retryAttemptCounter++;
                        Console.WriteLine($"[ProcessWithRetryableException] Execution attempt #{_retryAttemptCounter}");
                        await Task.Delay(100);
                        throw new RetryableException($"Simulated transient error for OrderId: {message.OrderId}");
                    });
                
                Console.WriteLine($"[ClientB] ? SUCCESS: {result}");
            }
            else if (message.ExceptionType == "circuit-breaker")
            {
                Console.WriteLine("[ClientB] Applying CIRCUIT BREAKER POLICY with Polly...");
                
                try
                {
                    var result = await _pollyPolicies.CircuitBreakerPipeline.ExecuteAsync<string>(
                        async (context) =>
                        {
                            _circuitBreakerAttemptCounter++;
                            Console.WriteLine($"[ProcessWithCircuitBreakerException] Execution attempt #{_circuitBreakerAttemptCounter}");
                            await Task.Delay(100);
                            throw new CircuitBreakerException($"Simulated critical error for OrderId: {message.OrderId}");
                        });
                    
                    Console.WriteLine($"[ClientB] ? SUCCESS: {result}");
                }
                catch (BrokenCircuitException ex)
                {
                    Console.WriteLine($"[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately");
                    Console.WriteLine($"[ClientB] Exception: {ex.Message}");
                    return false;
                }
            }
            else if (message.ExceptionType == "fatal")
            {
                Console.WriteLine("[ClientB] Processing with FATAL EXCEPTION (no resilience policy)...");
                Console.WriteLine($"[ProcessWithFatalException] Attempting to process order {message.OrderId}...");
                await Task.Delay(100);
                throw new FatalException($"Fatal error for OrderId: {message.OrderId} - Operation cannot be recovered");
            }
            else
            {
                Console.WriteLine($"[ProcessSuccessfully] Processing order {message.OrderId}...");
                await Task.Delay(500);
                var result = $"Order {message.OrderId} processed successfully by ClientB!";
                Console.WriteLine($"[ClientB] ? SUCCESS: {result}");
            }

            // INBOX PATTERN: Mark order as processed after successful handling
            await _inboxService.MarkAsProcessedAsync(message.OrderId);

            Console.WriteLine("===========================================");
            return true;
        }
        catch (RetryableException ex)
        {
            Console.WriteLine($"[ClientB] ? FAILED after all retry attempts");
            Console.WriteLine($"[ClientB] Final Exception: {ex.Message}");
            Console.WriteLine("===========================================");
            return false;
        }
        catch (CircuitBreakerException ex)
        {
            Console.WriteLine($"[ClientB] ? CIRCUIT BREAKER EXCEPTION - Circuit may open");
            Console.WriteLine($"[ClientB] Exception: {ex.Message}");
            Console.WriteLine("===========================================");
            return false;
        }
        catch (FatalException ex)
        {
            Console.WriteLine($"[ClientB] ? FATAL EXCEPTION - Failing immediately");
            Console.WriteLine($"[ClientB] Exception: {ex.Message}");
            Console.WriteLine("===========================================");
            return false;
        }
    }
}
