# CloudAMQP Setup Guide

This guide will help you configure the solution to use CloudAMQP so that SenderApp and ClientA can run on **different computers**.

## Why CloudAMQP?

CloudAMQP is a hosted RabbitMQ service that allows your applications to communicate over the internet without needing to expose your local RabbitMQ instance.

## Step 1: Create a CloudAMQP Account

1. Go to https://www.cloudamqp.com/
2. Click **Sign Up** (free account available)
3. Create an account (you can use GitHub/Google sign-in)

## Step 2: Create a RabbitMQ Instance

1. After logging in, click **Create New Instance**
2. Choose a plan:
   - **Little Lemur** (FREE) - Perfect for development and testing
   - Up to 1 million messages/month
3. Configure:
   - **Name**: e.g., "NServiceBusDemo"
   - **Region**: Choose closest to your location
   - **Data Center**: Any available
4. Click **Create instance**
5. Wait a few seconds for provisioning

## Step 3: Get Your Connection String

1. Click on your newly created instance
2. You'll see the **AMQP URL** (this is your connection string)
3. It looks like:
   ```
   amqps://username:password@instance.cloudamqp.com/vhost
   ```
4. **Copy this URL** - you'll need it for both applications

### Example CloudAMQP URL:
```
amqps://abcdefgh:xYz123-AbC456_DeF789@hawk.rmq.cloudamqp.com/abcdefgh
```

## Step 4: Configure SenderApp

### Option A: Using appsettings.json (Recommended)

1. Open `SenderApp/appsettings.json`
2. Update the RabbitMQ connection string:

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://YOUR_ACTUAL_URL_HERE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Option B: Using Environment Variable

Set an environment variable (useful for production):

**Windows (PowerShell):**
```powershell
$env:ConnectionStrings__RabbitMQ = "amqps://YOUR_ACTUAL_URL_HERE"
```

**Linux/Mac:**
```bash
export ConnectionStrings__RabbitMQ="amqps://YOUR_ACTUAL_URL_HERE"
```

## Step 5: Configure ClientA

### Option A: Using appsettings.json (Recommended)

1. Open `ClientA/appsettings.json`
2. Update with the **same** connection string:

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://YOUR_ACTUAL_URL_HERE"
  }
}
```

### Option B: Using Environment Variable

```powershell
$env:ConnectionStrings__RabbitMQ = "amqps://YOUR_ACTUAL_URL_HERE"
```

## Step 6: Deploy to Different Computers

### Computer 1 (SenderApp)

1. Copy the entire `SenderApp` folder to Computer 1
2. Ensure `appsettings.json` has the CloudAMQP URL
3. Run:
   ```powershell
   cd SenderApp
   dotnet run
   ```
4. Access Swagger at: https://localhost:5001/swagger

### Computer 2 (ClientA)

1. Copy the entire `ClientA` folder AND the `Messages` folder to Computer 2
2. Ensure `appsettings.json` has the CloudAMQP URL
3. Run:
   ```powershell
   cd ClientA
   dotnet run
   ```
4. You should see: "ClientA is running. Press any key to exit..."
5. It will show: "Connected to: CloudAMQP"

## Step 7: Test the Communication

1. On Computer 1, open Swagger (https://localhost:5001/swagger)
2. Execute the GET `/Order` endpoint
3. On Computer 2, watch the ClientA console
4. You should see the order being received! ??

```
===========================================
Order Received!
Order ID: 12345678-1234-1234-1234-123456789012
Details: Sample Order
Placed At: 2024-01-15 10:30:00
===========================================
```

## Monitoring Your Messages

### CloudAMQP Management UI

1. Go to your CloudAMQP instance dashboard
2. Click **RabbitMQ Manager**
3. This opens the RabbitMQ Management UI
4. You can see:
   - **Queues**: ClientA queue and messages
   - **Exchanges**: NServiceBus exchanges
   - **Connections**: Active connections from both apps
   - **Message rates**: Messages/second

### Key Things to Check

- **Queues** tab: Should show `ClientA` queue
- **Exchanges** tab: Should show `NServiceBus.Publish` exchange
- **Connections** tab: Should show 2 connections when both apps are running

## Troubleshooting

### "Connection failed" Error

1. **Check your connection string**
   - Make sure you copied the entire AMQP URL
   - Include the `amqps://` prefix
   - No spaces or line breaks

2. **Check CloudAMQP instance is running**
   - Log into CloudAMQP dashboard
   - Instance should show "Running"

3. **Firewall/Network**
   - CloudAMQP uses port 5671 (AMQPS)
   - Ensure it's not blocked

### Messages Not Being Received

1. **Check both apps are connected**
   - Look at CloudAMQP Connections tab
   - Should show 2 connections

2. **Check queue exists**
   - Go to Queues tab in RabbitMQ Manager
   - Should see `ClientA` queue

3. **Check for errors**
   - Look at console output from both apps
   - Check for NServiceBus errors

### Different Connection Strings in Each App

? **Problem**: Apps have different URLs  
? **Solution**: Both apps MUST use the exact same CloudAMQP URL

## Security Best Practices

### For Production:

1. **Never commit connection strings to Git**
   - Add to `.gitignore`:
     ```
     appsettings.json
     appsettings.*.json
     !appsettings.Development.json
     ```

2. **Use environment variables**
   - Set connection string via environment variables
   - Different per environment (dev, staging, prod)

3. **Use Secret Manager** (for local development)
   ```powershell
   dotnet user-secrets init --project SenderApp
   dotnet user-secrets set "ConnectionStrings:RabbitMQ" "amqps://..." --project SenderApp
   ```

4. **CloudAMQP Security Features**
   - Enable TLS (already included in `amqps://`)
   - Rotate passwords regularly
   - Use different instances for dev/prod

## Cost Considerations

### Free Tier (Little Lemur)
- ? 1 million messages/month
- ? Perfect for development and small projects
- ? No credit card required

### When to Upgrade
- More than 1 million messages/month
- Need higher throughput
- Need dedicated resources
- Production workloads

## Alternative: Self-Hosted RabbitMQ

If you don't want to use CloudAMQP, you can expose your local RabbitMQ:

1. **Port Forward RabbitMQ** (requires router access)
2. **Use VPN** (like Tailscale or Hamachi)
3. **Deploy RabbitMQ to a server** (Azure, AWS, DigitalOcean)

## Summary

? Create CloudAMQP account  
? Create instance and copy AMQP URL  
? Update `appsettings.json` in both SenderApp and ClientA  
? Deploy to different computers  
? Test and monitor via CloudAMQP dashboard  

**Connection String Format:**
```
amqps://username:password@hostname.cloudamqp.com/vhost
```

**Both apps must use the exact same connection string!**
