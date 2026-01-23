# Choreography vs Orchestration - Pattern Explanation

## ?? Your Solution Now Has BOTH Patterns!

### ?? Pattern Comparison

| Aspect | Choreography (ClientA) | Orchestration (ClientB) |
|--------|------------------------|-------------------------|
| **Communication** | Publish/Subscribe (Events) | Request/Response (Commands) |
| **Coupling** | Loose (no direct knowledge) | Tight (direct HTTP call) |
| **Coordination** | Decentralized | Centralized (SenderApp orchestrates) |
| **Technology** | NServiceBus + RabbitMQ | HTTP + Polly |
| **Discovery** | Subscribers self-register | Sender knows ClientB URL |
| **Example** | GET /Order ? Publishes event | POST /OrderProcess ? Calls ClientB |

---

## ?? Choreography Pattern (SenderApp ? ClientA)

### How It Works
```
SenderApp                  RabbitMQ                  ClientA
    ?                         ?                         ?
    ? Publish OrderPlaced     ?                         ?
    ???????????????????????????                         ?
    ?                         ? Deliver Event           ?
    ?                         ???????????????????????????
    ?                         ?                         ?  Handle
    ?                         ?                         ?  OrderPlaced
    ?                         ?                         ?
    
? SenderApp doesn't know ClientA exists
? ClientA subscribes independently
? Like dancers following music - no direct control
```

### Code Example
**SenderApp (OrderController.cs):**
```csharp
// Just publish the event - fire and forget!
await _messageSession.Publish(new OrderPlaced
{
    OrderId = orderId,
    OrderDetails = "Sample Order",
    PlacedAt = DateTime.UtcNow
});

return Accepted(); // Don't wait for processing
```

**ClientA subscribes automatically through NServiceBus**

### Characteristics
- ? **Decentralized**: No central coordinator
- ? **Loose Coupling**: Components don't know about each other
- ? **Scalable**: Easy to add more subscribers (ClientC, ClientD...)
- ? **Resilient**: If ClientA is down, message waits in queue
- ? **Async**: SenderApp doesn't wait for response

### When to Use
- ? Event notifications (order placed, user registered, etc.)
- ? Multiple consumers need same event
- ? Don't need immediate response
- ? Want loose coupling

---

## ?? Orchestration Pattern (SenderApp ? ClientB)

### How It Works
```
SenderApp                            ClientB
    ?                                   ?
    ? POST /process-order (Command)    ?
    ?????????????????????????????????????
    ?                                   ?  Process
    ?                                   ?  with Polly
    ?                                   ?  (retry/circuit breaker)
    ? Response (success/failure)        ?
    ?????????????????????????????????????
    ?                                   ?
    
? SenderApp knows ClientB endpoint
? SenderApp controls the flow
? Like a conductor directing an orchestra
```

### Code Example
**SenderApp (OrderProcessController.cs):**
```csharp
// Directly call ClientB and wait for response
var response = await client.PostAsJsonAsync(
    "http://localhost:5002/process-order", 
    command);

if (response.IsSuccessStatusCode)
{
    return Accepted(result); // Return response from ClientB
}
```

**ClientB exposes endpoint and handles request**

### Characteristics
- ? **Centralized**: SenderApp orchestrates the flow
- ? **Tight Coupling**: SenderApp knows ClientB URL and contract
- ? **Synchronous**: SenderApp waits for response (or timeout)
- ? **Control**: SenderApp handles success/failure
- ? **Resilience**: Polly provides retry & circuit breaker

### When to Use
- ? Need response from called service
- ? Complex workflows with dependencies
- ? Transaction-like behavior needed
- ? Want central control/visibility

---

## ?? Answer to Your Question: Is It Choreography?

### **NO - ClientB uses ORCHESTRATION!**

**Why?**
- ? SenderApp **directly calls** ClientB (knows the endpoint)
- ? SenderApp **waits for response** (synchronous)
- ? SenderApp **controls the flow** (orchestrator role)
- ? **Tight coupling** (HTTP endpoint dependency)

**What makes ClientA choreography:**
- ? SenderApp **publishes event** (doesn't know who listens)
- ? SenderApp **doesn't wait** (asynchronous)
- ? ClientA **independently subscribes** (self-service)
- ? **Loose coupling** (only message contract)

---

## ??? Your Architecture Now

```
????????????????
?  SenderApp   ? (Orchestrator + Event Publisher)
????????????????
       ?
       ????????????????????????????????????????????????????
       ?                         ?                        ?
       ? HTTP POST               ? Publish Event          ?
       ? (Orchestration)         ? (Choreography)         ?
       ?                         ?                        ?
       ?                         ?                        ?
???????????????          ???????????????                ?
?   ClientB   ?          ?  RabbitMQ   ?                ?
?  (Polly)    ?          ?             ?                ?
???????????????          ???????????????                ?
                                ?                        ?
                                ? Deliver Event          ?
                                ?                        ?
                                ?                        ?
                         ???????????????                ?
                         ?   ClientA   ?                ?
                         ?  (NServiceBus)                ?
                         ???????????????                ?
                                                        ?
```

---

## ?? Polly in ClientB

### What is Polly?
Polly is a .NET resilience library that provides:
- ? Retry policies
- ? Circuit breakers
- ? Timeout policies
- ? Fallback mechanisms
- ? And more...

### Your Configuration

#### 1. **Retry Policy** (for RetryableException)
```csharp
MaxRetryAttempts = 3
BackoffType = Exponential (1s, 2s, 4s)
ShouldHandle = RetryableException
```

**Flow:**
```
Request ? Fail ? Wait 1s ? Retry #1 ? Fail ? Wait 2s ? Retry #2 ? Fail ? Wait 4s ? Retry #3 ? Fail ? Return Error
```

#### 2. **Circuit Breaker** (for CircuitBreakerException)
```csharp
FailureRatio = 0.5 (50% failures)
MinimumThroughput = 3 requests
BreakDuration = 30 seconds
SamplingDuration = 30 seconds
```

**States:**
```
CLOSED (Normal)
   ? 3 failures
   ?
OPEN (Rejects all requests)
   ? After 30 seconds
   ?
HALF-OPEN (Tests with 1 request)
   ?
   ?? Success ??? CLOSED
   ?
   ?? Failure ??? OPEN
```

---

## ?? Testing Guide

### Test 1: Choreography (ClientA)
```powershell
# 1. Start ClientA
cd ClientA
dotnet run

# 2. Start SenderApp
cd SenderApp
dotnet run

# 3. Open Swagger: https://localhost:5001/swagger
# 4. Execute: GET /Order
# 5. Watch ClientA console receive the event
```

**Pattern:** Choreography ?

### Test 2: Orchestration Success (ClientB)
```powershell
# 1. Start ClientB
cd ClientB
dotnet run

# 2. SenderApp already running
# 3. Open Swagger: https://localhost:5001/swagger
# 4. Execute: POST /OrderProcess/success
# 5. Watch ClientB console process successfully
```

**Pattern:** Orchestration ?

### Test 3: Polly Retry (ClientB)
```powershell
# Execute: POST /OrderProcess/retry
# ClientB will:
#   1. Try processing ? FAIL
#   2. Wait 1s, retry #1 ? FAIL
#   3. Wait 2s, retry #2 ? FAIL
#   4. Wait 4s, retry #3 ? FAIL
#   5. Return 500 error
```

**Watch ClientB console for retry attempts!**

### Test 4: Circuit Breaker (ClientB)
```powershell
# Execute: POST /OrderProcess/circuit-breaker
# Do this 3-4 times quickly!
# 
# First 3 requests ? FAIL (circuit stays CLOSED)
# 4th request ? Circuit OPENS (503 Service Unavailable)
# Next requests ? Rejected immediately (circuit is OPEN)
# After 30 seconds ? Circuit goes HALF-OPEN ? Test 1 request
```

**Watch for "Circuit OPENED!" message!**

---

## ?? Endpoints Summary

### SenderApp (https://localhost:5001/swagger)

| Endpoint | Pattern | Description |
|----------|---------|-------------|
| GET /Order | **Choreography** | Publishes event to ClientA via RabbitMQ |
| POST /OrderProcess/success | **Orchestration** | Calls ClientB, processes successfully |
| POST /OrderProcess/retry | **Orchestration** | Calls ClientB, triggers retry policy (3 retries) |
| POST /OrderProcess/circuit-breaker | **Orchestration** | Calls ClientB, triggers circuit breaker |

### ClientA (Console - NServiceBus)
- Listens to RabbitMQ
- Processes OrderPlaced events
- **Choreography pattern**

### ClientB (http://localhost:5002/swagger)
- Exposes POST /process-order endpoint
- Uses Polly for resilience
- **Orchestration pattern**

---

## ?? Key Takeaways

### Choreography (ClientA)
? Event-driven  
? Loose coupling  
? RabbitMQ + NServiceBus  
? Asynchronous  
? Like dancers - everyone does their own thing  

### Orchestration (ClientB)
? Request/Response  
? Tight coupling  
? HTTP + Polly  
? Synchronous  
? Like conductor - central control  

### Both patterns have their place!
- Use **Choreography** for event notifications, loose coupling
- Use **Orchestration** when you need control, responses, transactions

---

## ?? Real-World Analogy

**Choreography (ClientA):**
> "I just got married!" (event)
> Friends and family decide on their own to send gifts, cards, congratulations

**Orchestration (ClientB):**
> "Hey Bob, can you process this order?" (command)
> Wait for Bob's response: "Done!" or "Failed!"

---

Your solution is a **hybrid** - it demonstrates BOTH patterns! ??
