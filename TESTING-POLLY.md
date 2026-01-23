# Testing Polly Resilience Policies in ClientB

## ?? What You're Testing

ClientB implements **Polly v8** with two resilience pipelines:

1. **Retry Policy** - Retries failed operations 3 times with exponential backoff
2. **Circuit Breaker** - Stops calling failing service after threshold

---

## ?? Setup

### Start the Applications

**Terminal 1 - ClientB:**
```powershell
cd ClientB
dotnet run
```

Expected output:
```
===========================================
ClientB is running (Web API with Polly)
Listening on: http://localhost:5002
Swagger: http://localhost:5002/swagger
===========================================

Polly Policies:
- RetryableException ? 3 retries with exponential backoff
- CircuitBreakerException ? Circuit breaker (3 failures opens circuit)
===========================================
```

**Terminal 2 - SenderApp:**
```powershell
cd SenderApp
dotnet run
```

---

## ? Test 1: Successful Processing (No Polly)

### Execute
1. Open https://localhost:5001/swagger
2. Expand `POST /OrderProcess/success`
3. Click **Try it out**
4. Click **Execute**

### Expected Response (202 Accepted)
```json
{
  "orderId": "abc-123...",
  "status": "Accepted",
  "processedBy": "ClientB",
  "exceptionType": "none",
  "result": {
    "message": "Order abc-123... processed successfully by ClientB",
    "orderId": "abc-123..."
  }
}
```

### ClientB Console Output
```
[PROCESS] Received order abc-123... with ExceptionType: none
[SUCCESS] Processing order abc-123...
[SUCCESS] Order abc-123... processed successfully!
```

**? No retries, no circuit breaker - smooth processing!**

---

## ?? Test 2: Retry Policy in Action

### Execute
1. Open https://localhost:5001/swagger
2. Expand `POST /OrderProcess/retry`
3. Click **Try it out**
4. Click **Execute**

### Expected Response (500 Internal Server Error)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Retryable Error",
  "status": 500,
  "detail": "Failed after retries: Simulated retryable failure for order abc-123..."
}
```

### ClientB Console Output
```
[PROCESS] Received order abc-123... with ExceptionType: retry
[RETRYABLE] Processing attempt #1
[RETRYABLE] Throwing RetryableException
[RETRY] Attempt 1 after 1s delay
[RETRYABLE] Processing attempt #2
[RETRYABLE] Throwing RetryableException
[RETRY] Attempt 2 after 2s delay
[RETRYABLE] Processing attempt #3
[RETRYABLE] Throwing RetryableException
[RETRY] Attempt 3 after 4s delay
[RETRYABLE] Processing attempt #4
[RETRYABLE] Throwing RetryableException
```

### What Happened?
1. **Initial attempt** ? Failed
2. **Retry #1** after 1 second ? Failed
3. **Retry #2** after 2 seconds (exponential backoff) ? Failed
4. **Retry #3** after 4 seconds (exponential backoff) ? Failed
5. **Give up** ? Return error to SenderApp

**Total:** 4 attempts (1 original + 3 retries) over ~7 seconds

---

## ? Test 3: Circuit Breaker - Opening the Circuit

### Execute (Do this 3-4 times quickly!)

1. Open https://localhost:5001/swagger
2. Expand `POST /OrderProcess/circuit-breaker`
3. Click **Try it out**
4. Click **Execute** - **REPEAT 4 TIMES QUICKLY!**

### First 3 Requests - Circuit CLOSED (Normal)

**Response (500 Internal Server Error):**
```json
{
  "title": "Circuit Breaker Error",
  "status": 500,
  "detail": "Circuit breaker triggered: Simulated circuit breaker failure..."
}
```

**ClientB Console:**
```
[PROCESS] Received order abc-1... with ExceptionType: circuit-breaker
[CIRCUIT BREAKER] Processing attempt #1
[CIRCUIT BREAKER] Throwing CircuitBreakerException

[PROCESS] Received order abc-2... with ExceptionType: circuit-breaker
[CIRCUIT BREAKER] Processing attempt #2
[CIRCUIT BREAKER] Throwing CircuitBreakerException

[PROCESS] Received order abc-3... with ExceptionType: circuit-breaker
[CIRCUIT BREAKER] Processing attempt #3
[CIRCUIT BREAKER] Throwing CircuitBreakerException
[CIRCUIT BREAKER] Circuit OPENED! Will remain open for 30 seconds.
```

### 4th Request - Circuit OPEN (Rejected)

**Response (503 Service Unavailable):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",
  "title": "Circuit Breaker Open",
  "status": 503,
  "detail": "Circuit breaker is OPEN. Service temporarily unavailable."
}
```

**ClientB Console:**
```
(No processing - request rejected immediately!)
```

### What Happened?
1. **Request 1-3:** Circuit is CLOSED, requests are processed and fail
2. **After 3 failures:** Circuit **OPENS** (threshold reached)
3. **Circuit OPEN:** All requests are **rejected immediately** without processing
4. **Purpose:** Protect ClientB from being overwhelmed with failing requests

**Circuit stays OPEN for 30 seconds, then goes to HALF-OPEN to test recovery**

---

## ?? Test 4: Circuit Breaker - Recovery (HALF-OPEN ? CLOSED)

### Execute (Wait 30 seconds after Test 3!)

After circuit opens, wait ~30 seconds, then:

1. Send **POST /OrderProcess/circuit-breaker** again

### Expected Behavior

**ClientB Console:**
```
[CIRCUIT BREAKER] Circuit HALF-OPEN. Testing if service recovered...
[PROCESS] Received order abc-4... with ExceptionType: circuit-breaker
[CIRCUIT BREAKER] Processing attempt #4
[CIRCUIT BREAKER] Throwing CircuitBreakerException
[CIRCUIT BREAKER] Circuit OPENED! Will remain open for 30 seconds.
```

**What Happened:**
1. After 30 seconds, circuit goes to **HALF-OPEN**
2. Circuit allows **1 test request** through
3. Request still fails (our simulation always fails)
4. Circuit goes back to **OPEN** for another 30 seconds

### To See Circuit CLOSE

You'd need to send a **successful request** when circuit is HALF-OPEN:
```powershell
# When circuit is HALF-OPEN, send success request:
POST /OrderProcess/success
```

Then:
```
[CIRCUIT BREAKER] Circuit CLOSED. Normal operation resumed.
```

---

## ?? Retry vs Circuit Breaker - Visual Comparison

### Retry Policy
```
Request ? Fail ? Wait 1s ? Retry #1 ? Fail ? Wait 2s ? Retry #2 ? Fail ? Wait 4s ? Retry #3 ? Fail ? Give Up

Purpose: Handle transient failures (temporary network glitch, etc.)
Best for: Quick recovery scenarios
```

### Circuit Breaker
```
CLOSED (Normal)
  ?
  ?? Success ??? Stay CLOSED
  ?
  ?? 3 Failures ??? OPEN
  ?
OPEN (Reject all)
  ?
  ?? Wait 30s ??? HALF-OPEN
  ?
HALF-OPEN (Test)
  ?
  ?? Success ??? CLOSED
  ?
  ?? Failure ??? OPEN

Purpose: Prevent cascading failures, give service time to recover
Best for: Protect from overwhelming a failing service
```

---

## ?? Interactive Testing Script

```powershell
# Test Script - Run all tests
Write-Host "Test 1: Success (no Polly)" -ForegroundColor Green
Invoke-WebRequest -Uri "https://localhost:5001/OrderProcess/success" -Method POST -SkipCertificateCheck
Start-Sleep -Seconds 2

Write-Host "`nTest 2: Retry Policy (3 retries)" -ForegroundColor Yellow
Invoke-WebRequest -Uri "https://localhost:5001/OrderProcess/retry" -Method POST -SkipCertificateCheck
Start-Sleep -Seconds 10

Write-Host "`nTest 3: Circuit Breaker (opening circuit)" -ForegroundColor Red
for ($i = 1; $i -le 4; $i++) {
    Write-Host "Request $i..."
    try {
        Invoke-WebRequest -Uri "https://localhost:5001/OrderProcess/circuit-breaker" -Method POST -SkipCertificateCheck
    } catch {
        Write-Host "  ? $($_.Exception.Response.StatusCode)"
    }
    Start-Sleep -Seconds 1
}

Write-Host "`nCircuit is now OPEN! Wait 30 seconds for HALF-OPEN state..." -ForegroundColor Magenta
```

---

## ?? Monitoring Circuit Breaker State

### Check Circuit State via ClientB Console

You'll see these messages:

**CLOSED ? OPEN:**
```
[CIRCUIT BREAKER] Circuit OPENED! Will remain open for 30 seconds.
```

**OPEN ? HALF-OPEN:**
```
[CIRCUIT BREAKER] Circuit HALF-OPEN. Testing if service recovered...
```

**HALF-OPEN ? CLOSED:**
```
[CIRCUIT BREAKER] Circuit CLOSED. Normal operation resumed.
```

**HALF-OPEN ? OPEN:**
```
[CIRCUIT BREAKER] Circuit OPENED! Will remain open for 30 seconds.
```

---

## ?? Key Points to Remember

### Retry Policy
- ? **3 retries** with exponential backoff (1s, 2s, 4s)
- ? Only for **RetryableException**
- ? **Total time:** ~7 seconds for all retries
- ? **Use when:** Expecting transient failures

### Circuit Breaker
- ? **Opens after:** 3 failures within 30 seconds
- ? **Stays open:** 30 seconds
- ? **Half-open:** Tests with 1 request
- ? Only for **CircuitBreakerException**
- ? **Use when:** Protecting against cascading failures

### Different from NServiceBus Retries!
- **Polly:** Application-level (in ClientB service logic)
- **NServiceBus:** Message-level (in ClientA message handling)

---

## ?? Summary

| Test | Endpoint | Exception | Polly Policy | Retries | Result |
|------|----------|-----------|--------------|---------|--------|
| 1 | /success | None | None | 0 | ? 202 Success |
| 2 | /retry | RetryableException | Retry | 3 | ? 500 Error after retries |
| 3 | /circuit-breaker | CircuitBreakerException | Circuit Breaker | 0 | ? 500 then 503 (circuit open) |

**Your ClientB is now resilient with Polly! ??**
