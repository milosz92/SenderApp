# Simple Setup Guide

## Step 1: Get CloudAMQP Connection String

1. Go to https://www.cloudamqp.com/
2. Sign up (free account available)
3. Create a new instance (choose "Little Lemur" - free)
4. Copy your **AMQP URL** (looks like: `amqps://username:password@instance.cloudamqp.com/vhost`)

## Step 2: Update Configuration

Update **BOTH** files with your CloudAMQP URL:

### SenderApp/appsettings.json
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://YOUR_ACTUAL_CLOUDAMQP_URL_HERE"
  }
}
```

### ClientA/appsettings.json
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://YOUR_ACTUAL_CLOUDAMQP_URL_HERE"
  }
}
```

?? **Both files must have the EXACT same URL!**

## Step 3: Run the Apps

### Computer 1 (SenderApp):
```powershell
cd SenderApp
dotnet run
```

Open: https://localhost:5001/swagger

### Computer 2 (ClientA):
```powershell
cd ClientA
dotnet run
```

Watch for messages in the console!

## Step 4: Test

1. On Computer 1: Open Swagger ? Execute GET /Order
2. On Computer 2: See the order appear in ClientA console! ??

---

## Local Testing (Same Computer)

If you want to test locally instead:

1. Start Docker RabbitMQ:
   ```powershell
   docker-compose up -d
   ```

2. Comment out CloudAMQP and uncomment localhost in **both** appsettings.json files:
   ```json
   {
     "ConnectionStrings": {
       // "RabbitMQ": "amqps://cloudamqp..."
       "RabbitMQ": "host=localhost;username=guest;password=guest"
     }
   }
   ```

3. Run both apps and test!

---

## Troubleshooting

**"Connection string is not configured" error:**
- Make sure you updated `appsettings.json` with your actual CloudAMQP URL
- Remove the placeholder text: `YOUR_USERNAME:YOUR_PASSWORD@YOUR_INSTANCE...`

**Messages not received:**
- Verify both apps have the **exact same** connection string
- Check CloudAMQP dashboard to see if both apps are connected

**"Connection refused":**
- Check your CloudAMQP URL is correct
- Make sure your CloudAMQP instance is running (check dashboard)
