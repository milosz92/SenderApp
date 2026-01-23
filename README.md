# How to Run the Application

## Prerequisites
- .NET 8 SDK installed
- RabbitMQ running (local or CloudAMQP)

## Run the 3 Applications (in separate terminals)

### Terminal 1: Start SenderApp (API)
```
cd SenderApp
dotnet run
```
API runs at: `http://localhost:5001`

### Terminal 2: Start ClientA
```
cd ClientA
dotnet run
```

### Terminal 3: Start ClientB
```
cd ClientB
dotnet run
```

---

## Send Test Requests

Open a browser or use curl/Postman to send POST requests:

### Test 1: Success ✅
```
POST http://localhost:5001/order/process
```
- ClientA: ✅ Logs order
- ClientB: ✅ Processes successfully

### Test 2: Retry 🔄
```
POST http://localhost:5001/order/process?exceptionType=retry
```
- ClientA: ✅ Logs order
- ClientB: 🔄 Retries 3 times (2s delay), then fails

### Test 3: Circuit Breaker ⚡
Send **5 times** to open the circuit:
```
POST http://localhost:5001/order/process?exceptionType=circuit-breaker
```
- ClientA: ✅ All succeed
- ClientB: ⚡ First 3 fail, circuit opens, last 2 rejected immediately

### Test 4: Fatal ❌
```
POST http://localhost:5001/order/process?exceptionType=fatal
```
- ClientA: ✅ Logs order
- ClientB: ❌ Fails immediately (no retry/circuit breaker)

---

## Exception Types

| Type | ClientA | ClientB |
|------|---------|---------|
| (none) | ✅ Success | ✅ Success |
| `retry` | ✅ Success | 🔄 Retry 3x → Error |
| `circuit-breaker` | ✅ Success | ⚡ Circuit opens |
| `fatal` | ✅ Success | ❌ Immediate fail |

---

## Stop Applications
Press any key in each terminal window

---

## Pattern: Choreography
- SenderApp **publishes** events
- ClientA & ClientB **subscribe** independently
- No direct coupling
- ClientB uses Polly for resilience
