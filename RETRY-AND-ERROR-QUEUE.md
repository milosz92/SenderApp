# Retry Policy and Error Queue Configuration

## Overview

Both SenderApp and ClientA are now configured with:
- ? **3 immediate retries** (no delay)
- ? **2 delayed retries** (10 seconds between attempts, increasing)
- ? **Error queue** for failed messages after all retries

---

## How It Works

### Message Processing Flow

```
Message Arrives
     ?
     ?
  Process
     ?
     ?? SUCCESS ?????????????????????? Done! ?
     ?
     ?? FAILURE
           ?
           ?
    Immediate Retry #1 (instant)
           ?
           ?? SUCCESS ??????????????? Done! ?
           ?
           ?? FAILURE
                 ?
                 ?
          Immediate Retry #2 (instant)
                 ?
                 ?? SUCCESS ????????? Done! ?
                 ?
                 ?? FAILURE
                       ?
                       ?
                Immediate Retry #3 (instant)
                       ?
                       ?? SUCCESS ??? Done! ?
                       ?
                       ?? FAILURE
                             ?
                             ?
                      Delayed Retry #1 (after 10 seconds)
                             ?
                             ?? SUCCESS ??? Done! ?
                             ?
                             ?? FAILURE
                                   ?
                                   ?
                            Delayed Retry #2 (after 20 seconds)
                                   ?
                                   ?? SUCCESS ??? Done! ?
                                   ?
                                   ?? FAILURE
                                         ?
                                         ?
                                  Moved to ERROR QUEUE ??
```

**Total Attempts:** 1 original + 3 immediate + 2 delayed = **6 attempts**

---

## Configuration Details

### Immediate Retries
```csharp
recoverability.Immediate(immediate =>
{
    immediate.NumberOfRetries(3);
});
```

- **What:** Retries happen instantly, one after another
- **When:** Good for transient errors (temporary network issues, etc.)
- **Count:** 3 retries
- **Delay:** None (immediate)

### Delayed Retries
```csharp
recoverability.Delayed(delayed =>
{
    delayed.NumberOfRetries(2);
    delayed.TimeIncrease(TimeSpan.FromSeconds(10));
});
```

- **What:** Retries with increasing delays between attempts
- **When:** Good for issues that need time to resolve (external service down, etc.)
- **Count:** 2 retries
- **Delays:**
  - 1st delayed retry: after 10 seconds
  - 2nd delayed retry: after 20 seconds (10 + 10)

### Error Queue
```csharp
endpointConfiguration.SendFailedMessagesTo("error");
```

- **What:** Queue where permanently failed messages go
- **Name:** `error`
- **When:** After all retries are exhausted
- **Action:** Messages wait here for manual intervention

---

## Your Question: Where Are Messages When ClientA Is Down?

### Scenario: ClientA is NOT running

When you send a message from SenderApp and ClientA is not running:

1. **SenderApp** publishes the `OrderPlaced` event to RabbitMQ ?
2. **RabbitMQ** stores the message in the **ClientA queue** ??
3. Message **waits** in the queue (durable, won't be lost)
4. When **ClientA starts**, it processes the waiting message ?

**Messages are NOT lost!** They are safely stored in RabbitMQ.

### How to Check in CloudAMQP

1. Go to your CloudAMQP dashboard
2. Click **RabbitMQ Manager**
3. Go to **Queues** tab
4. Look for the `ClientA` queue
5. You'll see:
   - **Ready:** Number of messages waiting to be processed
   - **Unacked:** Number of messages currently being processed

---

## Monitoring Queues

### CloudAMQP Dashboard

**View Your Queues:**
1. Login to https://customer.cloudamqp.com/
2. Click your instance
3. Click **RabbitMQ Manager**
4. Go to **Queues** tab

**You should see:**
- `ClientA` - Main queue for ClientA messages
- `error` - Error queue for failed messages (created automatically)
- `ClientA.timeouts` - For delayed retries (created automatically)

### Local RabbitMQ (Docker)

1. Open http://localhost:15672
2. Login: guest/guest
3. Click **Queues** tab
4. See the same queues

---

## Testing the Retry Logic

### Test 1: Normal Processing
```powershell
# 1. Start ClientA
cd ClientA
dotnet run

# 2. Send message from SenderApp (via Swagger)
# Result: Message processed successfully on first attempt ?
```

### Test 2: Message Waiting in Queue
```powershell
# 1. Make sure ClientA is NOT running
# 2. Send message from SenderApp (via Swagger)
# 3. Check CloudAMQP/RabbitMQ - message is in ClientA queue
# 4. Start ClientA
cd ClientA
dotnet run
# Result: Message is processed immediately ?
```

### Test 3: Retry Logic (Simulate Failure)

Temporarily modify the handler to fail:

```csharp
public Task Handle(OrderPlaced message, IMessageHandlerContext context)
{
    Console.WriteLine($"Processing order {message.OrderId}...");
    
    // Simulate a failure
    throw new Exception("Simulated failure for testing!");
    
    // This code won't be reached
    Console.WriteLine("Success!");
    return Task.CompletedTask;
}
```

**What will happen:**
1. First attempt: FAIL
2. Immediate retry #1: FAIL (instant)
3. Immediate retry #2: FAIL (instant)
4. Immediate retry #3: FAIL (instant)
5. Delayed retry #1: FAIL (after 10 seconds)
6. Delayed retry #2: FAIL (after 20 more seconds)
7. **Message moves to ERROR queue** ??

**You'll see in the logs:**
```
Immediate retry attempt: 0
Immediate retry attempt: 1
Immediate retry attempt: 2
Delayed retry attempt: 0
Delayed retry attempt: 1
Message moved to error queue
```

---

## Error Queue Management

### View Messages in Error Queue

**CloudAMQP/RabbitMQ Manager:**
1. Go to **Queues** tab
2. Click on **error** queue
3. Click **Get Messages**
4. See the failed message details

### Retry Failed Messages

**Option 1: ServicePulse (Advanced)**
- Install ServicePulse (NServiceBus monitoring tool)
- View and retry failed messages from UI

**Option 2: Manual (Simple)**
```powershell
# Fix the bug in your handler
# Restart ClientA
# Move messages from error queue back to ClientA queue (via RabbitMQ Manager)
```

**Option 3: Code-Based Retry**
You can write a small utility to move messages from error back to the main queue.

---

## Customizing Retry Policy

Want different retry settings? Update in `Program.cs`:

### More Immediate Retries
```csharp
immediate.NumberOfRetries(5); // Try 5 times instead of 3
```

### Longer Delays
```csharp
delayed.NumberOfRetries(3);
delayed.TimeIncrease(TimeSpan.FromMinutes(1)); // 1 min, 2 min, 3 min
```

### Disable Delayed Retries
```csharp
delayed.NumberOfRetries(0); // No delayed retries
```

### Custom Error Queue Name
```csharp
endpointConfiguration.SendFailedMessagesTo("my-custom-error-queue");
```

---

## Best Practices

? **Always configure error queue** - Don't lose failed messages  
? **Use immediate retries for transient errors** - Quick recovery  
? **Use delayed retries for external dependencies** - Give services time to recover  
? **Monitor error queue** - Check regularly for failures  
? **Set up alerts** - Get notified when messages fail  
? **Durable queues** - Messages survive RabbitMQ restart (already configured)  

---

## Summary

Your messages are now safe with:
- ?? **Durable queues** - Messages stored even if RabbitMQ restarts
- ?? **3 immediate retries** - Quick retry for transient errors
- ? **2 delayed retries** - Time-based retry for longer issues
- ?? **Error queue** - Failed messages don't disappear
- ?? **Full visibility** - Check queues in CloudAMQP/RabbitMQ Manager

**When ClientA is down:** Messages wait safely in the `ClientA` queue and process when it starts!
