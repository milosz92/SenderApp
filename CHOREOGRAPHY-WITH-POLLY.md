# Choreography Pattern with Polly in ClientB

## ? Architecture Pattern: **CHOREOGRAPHY**

This implementation uses **pure event-driven choreography** - no direct calls, no commands, just events.

## Architecture Overview

```
???????????????
?  SenderApp  ?
? (Publisher) ?
???????????????
       ?
       ? PUBLISH
       ? OrderPlaced Event
       ?
       ?
????????????????????????
?  RabbitMQ Exchange   ?
?   (Topic/Fanout)     ?
????????????????????????
       ?       ?
       ?       ? SUBSCRIBE
       ?       ?
       ?       ?
   ????????? ?????????????????????
   ?ClientA? ?     ClientB       ?
   ?       ? ?  (with Polly)     ?
   ????????? ?????????????????????
```

### Key Characteristics:

? **Event-Based**: SenderApp publishes `OrderPlaced` events
? **Decoupled**: Publisher doesn't know about subscribers
? **Multiple Subscribers**: Both ClientA and ClientB receive the same event
? **Independent Processing**: Each client processes the event independently
? **No Direct Calls**: No HTTP, no commands, pure pub/sub

## What Changed from Command Pattern

### Before (Command Pattern - NOT what you wanted):
```csharp
// SenderApp
await _messageSession.Send(new ProcessOrderCommand { ... });  // ? Direct send

// Routing required
routing.RouteToEndpoint(typeof(ProcessOrderCommand), "ClientB");

// HTTP calls
var response = await _httpClient.PostAsync("http://clientb/...");  // ? Direct call
```

### After (Choreography Pattern - What you have now):
```csharp
// SenderApp
await _messageSession.Publish(new OrderPlaced { ... });  // ? Event publish

// No routing needed - subscribers register themselves

// No HTTP calls - pure messaging
```

## How It Works

### 1. SenderApp Publishes Event
```csharp
[HttpPost("process")]
public async Task<IActionResult> ProcessOrder([FromQuery] string? exceptionType = null)
{
    var orderPlaced = new OrderPlaced
    {
        OrderId = Guid.NewGuid(),
        OrderDetails = "Order details",
        PlacedAt = DateTime.UtcNow,
        ExceptionType = exceptionType  // For testing Polly
    };

    await _messageSession.Publish(orderPlaced);  // ?? Publish event
}
```

### 2. ClientA Subscribes (Normal Processing)
```csharp
public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        // Process the order normally
        Console.WriteLine($"ClientA processing order {message.OrderId}");
        await Task.Delay(200);
        // No Polly, just normal processing
    }
}
```

### 3. ClientB Subscribes (With Polly)
```csharp
public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    private readonly PollyPolicies _pollyPolicies;

    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        if (message.ExceptionType == "retry")
        {
            // ?? Polly Retry Policy
            await _pollyPolicies.RetryPipeline.ExecuteAsync(
                async token => await ProcessWithRetryableException(message),
                context.CancellationToken);
        }
        else if (message.ExceptionType == "circuit-breaker")
        {
            // ? Polly Circuit Breaker Policy
            await _pollyPolicies.CircuitBreakerPipeline.ExecuteAsync(
                async token => await ProcessWithCircuitBreakerException(message),
                context.CancellationToken);
        }
        else
        {
            // Normal processing
            await ProcessSuccessfully(message);
        }
    }
}
```

## Polly Integration in ClientB

### Polly Policies (Inside ClientB Handler)

#### 1. Retry Policy
- **Trigger**: `ExceptionType = "retry"`
- **Max Attempts**: 3
- **Delay**: 2 seconds (constant)
- **Exception**: `RetryableException`

```csharp
RetryPipeline = new ResiliencePipelineBuilder<string>()
    .AddRetry(new RetryStrategyOptions<string>
    {
        ShouldHandle = new PredicateBuilder<string>()
            .Handle<RetryableException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        OnRetry = args =>
        {
            Console.WriteLine($"[POLLY RETRY] Attempt {args.AttemptNumber + 1} failed...");
            return ValueTask.CompletedTask;
        }
    })
    .Build();
```

#### 2. Circuit Breaker Policy
- **Trigger**: `ExceptionType = "circuit-breaker"`
- **Failure Threshold**: 3 consecutive failures
- **Break Duration**: 30 seconds
- **Exception**: `CircuitBreakerException`

```csharp
CircuitBreakerPipeline = new ResiliencePipelineBuilder<string>()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<string>
    {
        ShouldHandle = new PredicateBuilder<string>()
            .Handle<CircuitBreakerException>(),
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(30),
        OnOpened = args =>
        {
            Console.WriteLine("[POLLY CIRCUIT BREAKER] Circuit OPENED!");
            return ValueTask.CompletedTask;
        }
    })
    .Build();
```

## Testing Scenarios

### Test 1: Success (Both Clients)
```bash
POST /order/process
```

**Result**:
- ? ClientA: Processes successfully
- ? ClientB: Processes successfully (no exception)

### Test 2: Retry (ClientB only)
```bash
POST /order/process?exceptionType=retry
```

**Result**:
- ? ClientA: Processes successfully (ignores ExceptionType)
- ?? ClientB: Retries 3 times, then fails

**ClientB Output**:
```
[ClientB] Applying RETRY POLICY with Polly...
[ProcessWithRetryableException] Execution attempt #1
[POLLY RETRY] Attempt 1 failed. Retrying in 2s...

[ProcessWithRetryableException] Execution attempt #2
[POLLY RETRY] Attempt 2 failed. Retrying in 2s...

[ProcessWithRetryableException] Execution attempt #3
[POLLY RETRY] Attempt 3 failed. Retrying in 2s...

[ProcessWithRetryableException] Execution attempt #4
[ClientB] ? FAILED after all retry attempts
```

### Test 3: Circuit Breaker (ClientB only)
```bash
# Send 5 times
POST /order/process?exceptionType=circuit-breaker
```

**Result**:
- ? ClientA: All 5 events processed successfully
- ? ClientB: 
  - Events 1-3: Fail normally
  - After 3rd: Circuit OPENS
  - Events 4-5: Rejected immediately

**ClientB Output**:
```
// Event 1
[ProcessWithCircuitBreakerException] Execution attempt #1
[ClientB] ? CIRCUIT BREAKER EXCEPTION

// Event 2
[ProcessWithCircuitBreakerException] Execution attempt #2
[ClientB] ? CIRCUIT BREAKER EXCEPTION

// Event 3
[ProcessWithCircuitBreakerException] Execution attempt #3
[POLLY CIRCUIT BREAKER] Circuit OPENED! Break duration: 30s

// Event 4 (immediately rejected)
[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately

// Event 5 (immediately rejected)
[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately
```

## Why This is Choreography

| Aspect | Choreography (? Your Implementation) | Orchestration |
|--------|--------------------------------------|---------------|
| **Communication** | Events (Publish/Subscribe) | Commands (Send) |
| **Coupling** | Loose - Publisher doesn't know subscribers | Tight - Sender knows receiver |
| **Control** | Decentralized - Each service decides what to do | Centralized - Orchestrator controls flow |
| **Scalability** | Easy to add subscribers | Must update orchestrator |
| **Failure Handling** | Independent - Each service handles its own failures | Orchestrator must handle all failures |

## Files Involved

### Modified Files:
1. **Messages\OrderPlaced.cs** - Added `ExceptionType` property
2. **SenderApp\Controllers\OrderController.cs** - Publishes events (not commands)
3. **SenderApp\Program.cs** - Removed command routing
4. **ClientB\Program.cs** - Subscribe to events (not commands)
5. **ClientB\Handlers\OrderPlacedHandler.cs** - NEW handler with Polly

### Removed Files:
1. ~~**SenderApp\Controllers\OrderProcessController.cs**~~ - No HTTP calls needed
2. ~~**ClientB\Handlers\ProcessOrderCommandHandler.cs**~~ - No command handling
3. ~~**Messages\ProcessOrderCommand.cs**~~ - Can be deleted (optional)

### Polly Configuration:
- **ClientB\Services\PollyPolicies.cs** - Retry and Circuit Breaker policies
- **ClientB\appsettings.json** - Policy configuration

## Running the Demo

### Terminal 1: RabbitMQ
```bash
docker-compose up
```

### Terminal 2: SenderApp
```bash
cd SenderApp
dotnet run
```

### Terminal 3: ClientA
```bash
cd ClientA
dotnet run
```

### Terminal 4: ClientB
```bash
cd ClientB
dotnet run
```

### Terminal 5: Test
```bash
.\test-clientb.ps1
```

## Key Benefits

? **Pure Choreography**: No direct coupling, pure event-driven
? **Polly Inside Handler**: Resilience logic where it belongs
? **Single Controller**: One endpoint publishes different event types
? **Independent Scaling**: Add more subscribers without changing publisher
? **Flexible Testing**: Test different scenarios with query parameters

## Summary

**Before**: Command pattern with HTTP calls (tight coupling)
**Now**: Pure choreography with events + Polly resilience in ClientB

**Pattern**: ? **CHOREOGRAPHY** (Event-Driven, Pub/Sub)
**Polly**: ? **Inside ClientB handler** (not in HTTP layer)
**Controllers**: ? **Single OrderController** (publishes events)
**Coupling**: ? **Zero** (publisher doesn't know subscribers)

This is exactly what you wanted! ??
