# Running ClientB with Polly Resilience

## Prerequisites
1. RabbitMQ running (Docker or local)
2. .NET 8 SDK installed

## Quick Start

### 1. Start RabbitMQ
```powershell
docker-compose up -d
```

### 2. Start SenderApp (Terminal 1)
```powershell
cd SenderApp
dotnet run
```
SenderApp will start on `https://localhost:7294` and `http://localhost:5000`

### 3. Start ClientB (Terminal 2)
```powershell
cd ClientB
dotnet run
```

You should see:
```
===========================================
ClientB is running and listening for commands...
Connected to: localhost
Polly Policies:
  - Retry: 3 attempts with 2s delay
  - Circuit Breaker: 3 failures, 30s break
===========================================
```

### 4. Test the Implementation

#### Option A: Using PowerShell Test Script
```powershell
.\test-clientb.ps1
```

#### Option B: Using Swagger
1. Open browser: `https://localhost:7294/swagger`
2. Find `/order/process` endpoint
3. Test with different query parameters:
   - No parameter (success): `POST /order/process`
   - Retry test: `POST /order/process?exceptionType=retry`
   - Circuit breaker: `POST /order/process?exceptionType=circuit-breaker`

#### Option C: Using curl
```bash
# Success scenario
curl -X POST "https://localhost:7294/order/process" -k

# Retry scenario
curl -X POST "https://localhost:7294/order/process?exceptionType=retry" -k

# Circuit breaker scenario (run multiple times)
curl -X POST "https://localhost:7294/order/process?exceptionType=circuit-breaker" -k
```

## Expected Behaviors

### Success Scenario
**Request**: `POST /order/process`

**ClientB Console Output**:
```
[ClientB] Received ProcessOrderCommand
[ClientB] OrderId: <guid>
[ClientB] ExceptionType: none (success)
[ProcessSuccessfully] Processing order...
[ClientB] ? SUCCESS: Order <guid> processed successfully!
```

### Retry Scenario
**Request**: `POST /order/process?exceptionType=retry`

**ClientB Console Output**:
```
[ClientB] Received ProcessOrderCommand
[ClientB] Applying RETRY POLICY with Polly...
[ProcessWithRetryableException] Execution attempt #1
[POLLY RETRY] Attempt 1 failed. Retrying in 2s...
[POLLY RETRY] Exception: Simulated transient error...

[ProcessWithRetryableException] Execution attempt #2
[POLLY RETRY] Attempt 2 failed. Retrying in 2s...
[POLLY RETRY] Exception: Simulated transient error...

[ProcessWithRetryableException] Execution attempt #3
[POLLY RETRY] Attempt 3 failed. Retrying in 2s...
[POLLY RETRY] Exception: Simulated transient error...

[ProcessWithRetryableException] Execution attempt #4
[ClientB] ? FAILED after all retry attempts
```

### Circuit Breaker Scenario
**Request**: `POST /order/process?exceptionType=circuit-breaker` (send 3-5 times)

**ClientB Console Output**:
```
// First attempt
[ClientB] Applying CIRCUIT BREAKER POLICY with Polly...
[ProcessWithCircuitBreakerException] Execution attempt #1
[ClientB] ? CIRCUIT BREAKER EXCEPTION - Circuit may open

// Second attempt
[ProcessWithCircuitBreakerException] Execution attempt #2
[ClientB] ? CIRCUIT BREAKER EXCEPTION - Circuit may open

// Third attempt - Circuit opens!
[ProcessWithCircuitBreakerException] Execution attempt #3
[POLLY CIRCUIT BREAKER] Circuit OPENED! Break duration: 30s
[ClientB] ? CIRCUIT BREAKER EXCEPTION - Circuit may open

// Fourth attempt - Rejected immediately
[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately
Exception: The circuit is now open and is not allowing calls.
```

## Architecture Comparison

### ClientA (Choreography - Event-Driven)
```
SenderApp
    ??> PUBLISH OrderPlaced Event
         ??> RabbitMQ Exchange
              ??> ClientA (subscribes and handles)
              ??> (Other potential subscribers)
```
- **Pattern**: Pub/Sub (Choreography)
- **Coupling**: Loose - Publisher doesn't know subscribers
- **Resilience**: NServiceBus built-in retries

### ClientB (Command - Direct Messaging)
```
SenderApp
    ??> SEND ProcessOrderCommand
         ??> ClientB Queue
              ??> ClientB (handles command)
```
- **Pattern**: Command (Point-to-Point)
- **Coupling**: Tight - Sender knows receiver
- **Resilience**: Polly (Retry + Circuit Breaker)

## Configuration Files

### SenderApp/appsettings.json
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  }
}
```

### ClientB/appsettings.json
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  },
  "Polly": {
    "RetryPolicy": {
      "MaxRetryAttempts": 3,
      "DelayBetweenRetriesSeconds": 2
    },
    "CircuitBreaker": {
      "FailureThreshold": 3,
      "DurationOfBreakSeconds": 30,
      "SamplingDurationSeconds": 60
    }
  }
}
```

## RabbitMQ Queues Created

After running the applications, you should see these queues in RabbitMQ Management UI (`http://localhost:15672`):

1. **ClientA** - Receives OrderPlaced events (quorum queue)
2. **ClientB** - Receives ProcessOrderCommand messages (quorum queue)
3. **error** - Dead letter queue for failed messages
4. **SenderApp** - SenderApp's input queue (though it doesn't handle messages)

## Troubleshooting

### RabbitMQ Connection Issues
If you see "RabbitMQ connection string is not configured":
1. Check `appsettings.json` in both SenderApp and ClientB
2. Ensure RabbitMQ is running: `docker ps`
3. Test RabbitMQ: Open `http://localhost:15672` (guest/guest)

### Messages Not Being Received
1. Check RabbitMQ Management UI for queues
2. Verify queue bindings and exchanges
3. Check console for exceptions

### Circuit Breaker Not Opening
- Ensure you send at least 3 requests with `exceptionType=circuit-breaker`
- The circuit requires 3 consecutive failures to open
- Watch ClientB console for the "Circuit OPENED" message

## Next Steps

1. **Modify Retry Logic**: Change retry count in `appsettings.json`
2. **Adjust Circuit Breaker**: Change failure threshold or break duration
3. **Add More Policies**: Combine retry + circuit breaker, add timeout policies
4. **Real-World Scenarios**: Replace simulated exceptions with actual service calls
5. **Monitoring**: Add logging/telemetry to track policy executions

## Answer to Your Question

**Is ClientB using Choreography Pattern?**

**NO** - ClientB uses the **Command Pattern** (also called Point-to-Point or Send pattern).

**Why?**
- SenderApp **SENDS** a command to a specific destination (ClientB)
- SenderApp knows about ClientB (tight coupling)
- Only one handler can process the command

**ClientA** (which you already have) uses **Choreography** because:
- SenderApp **PUBLISHES** an event
- SenderApp doesn't know who will handle it
- Multiple subscribers can react to the same event

Both patterns are valid! Use:
- **Commands** when you need guaranteed delivery to a specific handler
- **Events** when multiple systems need to react independently
