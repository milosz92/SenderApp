# NServiceBus Choreography Pattern Demo

This solution demonstrates a basic choreography pattern using NServiceBus and RabbitMQ.

## Architecture

- **SenderApp**: ASP.NET Core Web API with Swagger that publishes `OrderPlaced` events
- **ClientA**: Console application that subscribes to and handles `OrderPlaced` events
- **Messages**: Shared message contracts library
- **RabbitMQ**: Message broker (CloudAMQP or local Docker)

## Features

? Publish/Subscribe choreography pattern  
? Swagger UI for easy testing  
? **3 immediate retries + 2 delayed retries**  
? **Error queue for failed messages**  
? Durable queues (messages survive restarts)  
? CloudAMQP support for remote deployment  

## Prerequisites

- .NET 8 SDK
- CloudAMQP account (free) OR Docker Desktop (for local testing)

---

## ?? Quick Start (CloudAMQP - Different Computers)

### 1. Get CloudAMQP Connection

1. Create free account at https://www.cloudamqp.com/
2. Create instance (Little Lemur - free)
3. Copy your AMQP URL

### 2. Configure Both Apps

Update with your CloudAMQP URL in **BOTH** files:

**SenderApp/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://your-actual-url-here"
  }
}
```

**ClientA/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://your-actual-url-here"
  }
}
```

?? **Both must have the EXACT same URL!**

### 3. Run on Different Computers

**Computer 1 (SenderApp):**
```powershell
cd SenderApp
dotnet run
```
Open: https://localhost:5001/swagger

**Computer 2 (ClientA):**
```powershell
cd ClientA
dotnet run
```

### 4. Test

1. On Computer 1: Swagger ? Execute GET /Order
2. On Computer 2: Watch ClientA console receive the message! ??

---

## ?? Local Testing (Same Computer)

### 1. Start RabbitMQ

```powershell
docker-compose up -d
```

### 2. Update Both appsettings.json

Change to localhost connection:

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  }
}
```

### 3. Run Both Apps

**Terminal 1:**
```powershell
cd ClientA
dotnet run
```

**Terminal 2:**
```powershell
cd SenderApp
dotnet run
```

### 4. Test with Swagger

Open: https://localhost:5001/swagger

Execute GET /Order ? Watch ClientA console!

---

## ?? Retry Policy & Error Handling

### Automatic Retry Configuration

Both apps are configured with automatic retries:

- **3 immediate retries** (instant, for transient errors)
- **2 delayed retries** (10s, 20s delays for longer issues)
- **Error queue** for permanently failed messages

**Total:** Up to 6 attempts before moving to error queue

### What Happens When ClientA Is Not Running?

? **Messages are NOT lost!**

1. SenderApp publishes event to RabbitMQ
2. RabbitMQ stores message in `ClientA` queue
3. Message waits (durable, survives restarts)
4. When ClientA starts, it processes waiting messages

### Monitor Queues

**CloudAMQP:**
- Dashboard ? RabbitMQ Manager ? Queues
- See `ClientA` queue (ready messages)
- See `error` queue (failed messages)

**Local RabbitMQ:**
- http://localhost:15672 (guest/guest)
- Queues tab

?? **See [RETRY-AND-ERROR-QUEUE.md](RETRY-AND-ERROR-QUEUE.md) for detailed information**

---

## ?? Expected Output

### SenderApp (API Response - 202 Accepted):
```json
{
  "orderId": "12345678-1234-1234-1234-123456789012",
  "status": "Accepted"
}
```

### ClientA (Console Output):
```
===========================================
ClientA is running and listening for messages...
Connected to: CloudAMQP
Retry Policy: 3 immediate retries + 2 delayed retries
Error Queue: 'error'
Press any key to exit...
===========================================
===========================================
Order Received!
Order ID: 12345678-1234-1234-1234-123456789012
Details: Sample Order
Placed At: 2024-01-15 10:30:00
===========================================
```

---

## ?? Configuration Files

Each app has **one** `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://user:pass@instance.cloudamqp.com/vhost"
  },
  "_comment": "To use local Docker RabbitMQ instead, change to: host=localhost;username=guest;password=guest"
}
```

To switch between CloudAMQP and local:
- Update the `RabbitMQ` connection string
- Change in **both** SenderApp and ClientA

---

## ?? Monitoring

### CloudAMQP Dashboard
- Login to https://customer.cloudamqp.com/
- Click your instance ? RabbitMQ Manager
- View queues, exchanges, connections
- Monitor message rates and errors

### Local RabbitMQ
- Open http://localhost:15672
- Login: guest/guest
- View queues and exchanges

### Queues You'll See
- `ClientA` - Main message queue
- `error` - Failed messages after all retries
- `ClientA.timeouts` - For delayed retries

---

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| "Connection string is not configured" | Update `appsettings.json` with your actual CloudAMQP URL |
| Messages not received | Verify **same** connection string in both apps |
| Connection refused (local) | Run `docker-compose up -d` first |
| Connection refused (CloudAMQP) | Check URL is correct in appsettings.json |
| Messages disappear | Check `error` queue in RabbitMQ Manager |
| Retries not working | Messages are retrying - check CloudAMQP logs |

---

## ?? Clean Up

Stop apps: `Ctrl+C` in each terminal

**Local Docker:**
```powershell
docker-compose down
```

---

## ?? Additional Documentation

- **SETUP.md** - Simple setup guide
- **RETRY-AND-ERROR-QUEUE.md** - Retry policy and error handling details
- **ARCHITECTURE.md** - Visual diagrams
- **QUICK-REFERENCE.md** - Command reference
- **APPSETTINGS-EXAMPLES.md** - Configuration examples

---

## ? Test Scripts

**Test API:**
```powershell
.\test-order.ps1
```

**Send 5 orders:**
```powershell
.\test-order.ps1 -Count 5
```

---

## ?? Key Points

? One `appsettings.json` per app  
? CloudAMQP URL or localhost connection  
? Both apps must use the **same** connection string  
? Swagger at: https://localhost:5001/swagger  
? Messages are durable - survive restarts  
? 3 immediate + 2 delayed retries  
? Error queue for failed messages  
? No code changes to switch between local/CloudAMQP
