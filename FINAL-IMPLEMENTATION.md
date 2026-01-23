# ? Implementation Complete: Pure Choreography with Polly

## What You Asked For:
> "I want to have no direct call for both A and B, I want to use choreograph, just inside the B I want to have polly with circuit breaker. So I guess it can be even in one controller?"

## What You Got:

### ? No Direct Calls
- **Removed**: HTTP calls from SenderApp to ClientB
- **Removed**: `OrderProcessController.cs` 
- **Using**: Pure NServiceBus messaging (events only)

### ? Choreography Pattern
- **SenderApp**: Publishes `OrderPlaced` events (doesn't know about subscribers)
- **ClientA**: Subscribes to `OrderPlaced` (processes independently)
- **ClientB**: Subscribes to `OrderPlaced` (processes with Polly)
- **No coupling**: Publisher and subscribers are completely decoupled

### ? Polly Inside ClientB
- **Location**: `ClientB\Handlers\OrderPlacedHandler.cs`
- **Retry Policy**: 3 attempts with 2-second delays
- **Circuit Breaker**: Opens after 3 failures, stays open 30 seconds
- **Configured in**: `ClientB\appsettings.json`

### ? Single Controller
- **File**: `SenderApp\Controllers\OrderController.cs`
- **Endpoints**:
  - `GET /order` - Simple event publish
  - `POST /order/process?exceptionType=X` - Test different scenarios

## Architecture

```
SenderApp (One Controller)
    ?
    ??? PUBLISH OrderPlaced Event
            ?
            ??? ClientA (normal processing)
            ?
            ??? ClientB (Polly resilience)
                    ??? Retry Policy (3 attempts)
                    ??? Circuit Breaker (opens after 3 failures)
```

## Test Commands

```bash
# Success (both clients process)
curl -X POST "https://localhost:7294/order/process" -k

# Retry in ClientB only (ClientA processes normally)
curl -X POST "https://localhost:7294/order/process?exceptionType=retry" -k

# Circuit breaker in ClientB only (ClientA processes normally)
curl -X POST "https://localhost:7294/order/process?exceptionType=circuit-breaker" -k
```

## What Changed

| Before | After |
|--------|-------|
| ? Command pattern (Send) | ? Choreography (Publish) |
| ? HTTP calls to ClientB | ? Pure messaging |
| ? Two controllers | ? One controller |
| ? Tight coupling | ? Zero coupling |
| ? ProcessOrderCommand | ? OrderPlaced event |

## Files Modified

**Created:**
- `ClientB\Handlers\OrderPlacedHandler.cs` - Event handler with Polly

**Modified:**
- `Messages\OrderPlaced.cs` - Added `ExceptionType` property
- `SenderApp\Controllers\OrderController.cs` - Single controller, publishes events
- `SenderApp\Program.cs` - Removed command routing
- `ClientB\Program.cs` - Subscribe to events

**Removed:**
- `SenderApp\Controllers\OrderProcessController.cs` - No HTTP calls needed
- `ClientB\Handlers\ProcessOrderCommandHandler.cs` - No commands

## Running the Solution

```bash
# Terminal 1
docker-compose up

# Terminal 2
cd SenderApp && dotnet run

# Terminal 3
cd ClientA && dotnet run

# Terminal 4
cd ClientB && dotnet run

# Terminal 5
.\test-clientb.ps1
```

## Summary

? **Pure choreography** - No direct calls anywhere
? **Polly inside ClientB** - Circuit breaker in event handler
? **Single controller** - `OrderController` publishes all events
? **Both A and B** - Subscribe to same event, process independently

**This is exactly what you wanted!** ??

See `CHOREOGRAPHY-WITH-POLLY.md` for detailed documentation.
