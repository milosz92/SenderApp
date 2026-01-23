# Quick Start: Pure Choreography with Polly

## What You Have Now

? **Pure Choreography** - No direct calls, only events
? **Polly in ClientB** - Circuit breaker inside event handler  
? **Single Controller** - `OrderController` publishes all events
? **Both A & B subscribe** - Independent event processing

## Start Everything

### Option 1: Manual Start (Recommended for Learning)

```bash
# Terminal 1: RabbitMQ
docker-compose up

# Terminal 2: SenderApp
cd SenderApp
dotnet run
# Should start on https://localhost:7294

# Terminal 3: ClientA
cd ClientA
dotnet run
# Watch console for: "ClientA is running and listening for messages..."

# Terminal 4: ClientB  
cd ClientB
dotnet run
# Watch console for: "ClientB is running and listening for events..."
```

### Option 2: PowerShell Script
```powershell
.\start-demo.ps1
```

## Test the Implementation

### Using PowerShell Test Script (Easiest)
```powershell
.\test-clientb.ps1
```

### Using Swagger UI
1. Open: `https://localhost:7294/swagger`
2. Find: `POST /order/process`
3. Try these query parameters:
   - (empty) - Success scenario
   - `?exceptionType=retry` - Retry test
   - `?exceptionType=circuit-breaker` - Circuit breaker test

### Using curl

```bash
# Test 1: Success (both clients process normally)
curl -X POST "https://localhost:7294/order/process" -k

# Test 2: Retry in ClientB (ClientA processes normally)
curl -X POST "https://localhost:7294/order/process?exceptionType=retry" -k

# Test 3: Circuit Breaker in ClientB (run 5 times)
for i in {1..5}; do
  curl -X POST "https://localhost:7294/order/process?exceptionType=circuit-breaker" -k
  sleep 1
done
```

## What to Watch

### Test 1: Success ?
**Expected in ClientA console**:
```
[ClientA] Received OrderPlaced Event
[ClientA] OrderId: <guid>
[ClientA] Processing order...
[ClientA] ? Order processed successfully
```

**Expected in ClientB console**:
```
[ClientB] Received OrderPlaced Event
[ClientB] OrderId: <guid>
[ClientB] ExceptionType: none (success)
[ProcessSuccessfully] Processing order...
[ClientB] ? SUCCESS: Order processed successfully!
```

### Test 2: Retry ??
**Expected in ClientA console**:
```
[ClientA] Received OrderPlaced Event
[ClientA] ? Order processed successfully
```

**Expected in ClientB console**:
```
[ClientB] Received OrderPlaced Event
[ClientB] ExceptionType: retry
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

### Test 3: Circuit Breaker ?
**Expected in ClientA console** (for all 5 events):
```
[ClientA] Received OrderPlaced Event
[ClientA] ? Order processed successfully
(x5 times)
```

**Expected in ClientB console**:
```
// Event 1
[ClientB] Applying CIRCUIT BREAKER POLICY with Polly...
[ProcessWithCircuitBreakerException] Execution attempt #1
[ClientB] ? CIRCUIT BREAKER EXCEPTION

// Event 2
[ProcessWithCircuitBreakerException] Execution attempt #2
[ClientB] ? CIRCUIT BREAKER EXCEPTION

// Event 3
[ProcessWithCircuitBreakerException] Execution attempt #3
[POLLY CIRCUIT BREAKER] Circuit OPENED! Break duration: 30s
[ClientB] ? CIRCUIT BREAKER EXCEPTION

// Event 4
[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately

// Event 5
[ClientB] ? CIRCUIT BREAKER IS OPEN - Request rejected immediately
```

## Verify in RabbitMQ

Open: `http://localhost:15672` (guest/guest)

**Queues you should see**:
- `ClientA` - Receives OrderPlaced events
- `ClientB` - Receives OrderPlaced events
- `error` - Failed messages (after retries exhausted)
- `SenderApp` - Publisher's queue

**Exchanges**:
Look for topic/fanout exchange handling OrderPlaced events

## Troubleshooting

### "Connection refused" errors
- Check RabbitMQ is running: `docker ps`
- Check connection string in `appsettings.json`

### ClientA or ClientB not receiving events
- Check console output for "listening for events"
- Check RabbitMQ Management UI for bindings
- Restart the client to re-subscribe

### Circuit stays closed
- Must send at least 3 requests with `circuit-breaker` exception type
- Wait 1 second between requests
- Check ClientB console for circuit state changes

### Messages go to error queue immediately
- This is expected after retries are exhausted
- Check error queue in RabbitMQ Management UI
- Messages can be replayed from there

## Configuration

### Polly Settings (ClientB\appsettings.json)
```json
{
  "Polly": {
    "RetryPolicy": {
      "MaxRetryAttempts": 3,          // Change to retry more/less
      "DelayBetweenRetriesSeconds": 2  // Change delay between retries
    },
    "CircuitBreaker": {
      "FailureThreshold": 3,           // Failures before circuit opens
      "DurationOfBreakSeconds": 30,    // How long circuit stays open
      "SamplingDurationSeconds": 60    // Time window for failure counting
    }
  }
}
```

## Next Steps

1. **Modify retry count** - Change in `appsettings.json`, restart ClientB
2. **Add more subscribers** - Create ClientC that also subscribes to OrderPlaced
3. **Real scenarios** - Replace simulated exceptions with actual business logic
4. **Monitoring** - Add Application Insights or logging
5. **Different events** - Add OrderCancelled, OrderShipped events

## Architecture Summary

```
SenderApp (1 Controller)
    ?? Publishes OrderPlaced Event
            ?? ClientA (normal processing)
            ?? ClientB (Polly resilience)
```

**Pattern**: Pure Choreography (Event-Driven)
**Coupling**: Zero (decoupled)
**Polly**: Inside ClientB event handler
**Tests**: 3 scenarios (success, retry, circuit breaker)

## Documentation Files

- `FINAL-IMPLEMENTATION.md` - Summary of changes
- `CHOREOGRAPHY-WITH-POLLY.md` - Detailed explanation
- `ARCHITECTURE-VISUAL.md` - Visual diagrams
- `test-clientb.ps1` - Automated testing script

**You're ready to go!** ??
