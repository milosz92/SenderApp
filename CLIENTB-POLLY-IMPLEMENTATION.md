# ClientB with Polly Implementation

## Overview
ClientB has been implemented as an NServiceBus endpoint that processes `ProcessOrderCommand` messages with Polly resilience patterns including retry and circuit breaker policies.

## Architecture Pattern: **Command Pattern (NOT Choreography)**

### What was implemented:
- **SenderApp** sends a **Command** (`ProcessOrderCommand`) to **ClientB**
- **ClientB** handles the command with Polly policies

### Is this Choreography? **NO**

**This is the COMMAND pattern** (also called "send" pattern), not choreography.

#### Command Pattern (What we implemented):
- ? **Point-to-point communication**: SenderApp SENDS command ? ClientB
- ? **Explicit destination**: SenderApp knows exactly where to send (ClientB queue)
- ? **Direct responsibility**: SenderApp tells ClientB what to do
- ? **Coupling**: SenderApp knows about ClientB's existence

#### Choreography Pattern (What we have with ClientA):
- ? **Event-based**: SenderApp PUBLISHES event ? Multiple subscribers
- ? **No explicit destination**: Publisher doesn't know who will handle it
- ? **Implicit responsibility**: Subscribers decide to react to events
- ? **Decoupling**: Publisher doesn't know about subscribers

### Example in your solution:

**Choreography** (ClientA):
```
SenderApp --PUBLISH OrderPlaced Event--> [RabbitMQ Exchange]
                                              |
                                              ??> ClientA (subscriber)
                                              ??> (Other potential subscribers)
```

**Command** (ClientB):
```
SenderApp --SEND ProcessOrderCommand--> ClientB Queue --> ClientB
```

## Polly Resilience Policies

### 1. Retry Policy
**Triggered by**: `ExceptionType = "retry"`
- **MaxRetryAttempts**: 3 (configured in appsettings.json)
- **Delay**: 2 seconds constant delay between retries
- **Exception Type**: `RetryableException`
- **Behavior**: Automatically retries the operation 3 times before failing

**Use case**: Transient failures like temporary network issues, database timeouts

### 2. Circuit Breaker Policy
**Triggered by**: `ExceptionType = "circuit-breaker"`
- **FailureThreshold**: 3 consecutive failures
- **BreakDuration**: 30 seconds
- **SamplingDuration**: 60 seconds
- **Exception Type**: `CircuitBreakerException`
- **Behavior**: 
  - After 3 failures, circuit opens (rejects all requests immediately)
  - Stays open for 30 seconds
  - Transitions to half-open to test if service recovered
  - Closes if test succeeds, re-opens if fails

**Use case**: Preventing cascading failures when a downstream service is down

## Testing the Implementation

### 1. Success Scenario
```
POST http://localhost:5000/order/process
```
? Order processes successfully

### 2. Retry Scenario
```
POST http://localhost:5000/order/process?exceptionType=retry
```
Expected behavior:
1. First attempt fails ? Retry in 2s
2. Second attempt fails ? Retry in 2s
3. Third attempt fails ? Retry in 2s
4. Fourth attempt fails ? Give up, send to error queue

### 3. Circuit Breaker Scenario
```
POST http://localhost:5000/order/process?exceptionType=circuit-breaker
```
Expected behavior:
1. First call fails
2. Second call fails
3. Third call fails ? **Circuit OPENS**
4. Further calls rejected immediately with `BrokenCircuitException`
5. After 30 seconds ? Circuit transitions to HALF-OPEN
6. Test call determines if circuit closes or re-opens

## Configuration

### appsettings.json
```json
{
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

## Files Created

1. **ClientB\Program.cs** - NServiceBus endpoint setup with Polly registration
2. **ClientB\Services\PollyPolicies.cs** - Polly policy configuration
3. **ClientB\Handlers\ProcessOrderCommandHandler.cs** - Message handler with policy execution
4. **ClientB\appsettings.json** - Configuration for Polly policies
5. **Messages\ProcessOrderCommand.cs** - Command message definition
6. **Messages\CustomExceptions.cs** - Custom exception types for different policies

## NServiceBus Configuration

- **Endpoint Name**: ClientB
- **Transport**: RabbitMQ with Quorum queues
- **Serialization**: System.Text.Json
- **NServiceBus Retries**: **DISABLED** (using Polly instead)
  - Immediate Retries: 0
  - Delayed Retries: 0
- **Error Queue**: "error"

## Key Differences: Polly vs NServiceBus Retries

| Feature | NServiceBus Retries | Polly |
|---------|-------------------|-------|
| **Control** | Limited configuration | Full control over policies |
| **Visibility** | Less logging | Rich logging with callbacks |
| **Flexibility** | Fixed patterns | Custom strategies |
| **Circuit Breaker** | Not included | Built-in |
| **Exception Types** | All exceptions | Specific exception types |
| **Integration** | NServiceBus only | Can use anywhere in code |

## Summary

? **ClientB**: Command-driven endpoint with Polly resilience
? **Polly Retry**: Handles transient failures
? **Polly Circuit Breaker**: Prevents cascading failures
? **Pattern**: **Command (NOT Choreography)**
? **Compared to ClientA**: ClientA uses Choreography (pub/sub), ClientB uses Command (point-to-point)
