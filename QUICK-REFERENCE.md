# Quick Reference Card

## ?? Run Locally (Same Computer)

### Option 1: Manual
```powershell
# Terminal 1: Start RabbitMQ
docker-compose up -d

# Terminal 2: Start ClientA
cd ClientA
dotnet run

# Terminal 3: Start SenderApp
cd SenderApp
dotnet run

# Browser: Test with Swagger
# Open: https://localhost:5001/swagger
```

### Option 2: Using Scripts
```powershell
# Setup and build
.\start-demo.ps1

# Then in separate terminals:
cd ClientA && dotnet run
cd SenderApp && dotnet run

# Test
.\test-order.ps1
```

---

## ?? Run on Different Computers

### Setup (One-time)
1. Create CloudAMQP account: https://www.cloudamqp.com/
2. Create instance ? Copy AMQP URL
3. Update both `appsettings.json` files with the CloudAMQP URL

### Computer 1 (SenderApp)
```json
// SenderApp/appsettings.json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://user:pass@instance.cloudamqp.com/vhost"
  }
}
```

```powershell
cd SenderApp
dotnet run
# Swagger: https://localhost:5001/swagger
```

### Computer 2 (ClientA)
```json
// ClientA/appsettings.json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://user:pass@instance.cloudamqp.com/vhost"
  }
}
```

```powershell
cd ClientA
dotnet run
# Watch console for messages
```

?? **Both must use the EXACT same CloudAMQP URL!**

---

## ?? Important URLs

### Local Development
- **Swagger UI**: https://localhost:5001/swagger
- **API Endpoint**: https://localhost:5001/Order
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### CloudAMQP
- **Dashboard**: https://customer.cloudamqp.com/
- **RabbitMQ Manager**: Dashboard ? Instance ? RabbitMQ Manager

---

## ?? Configuration Files

### Switch Between Local and CloudAMQP

**For Local (Docker RabbitMQ):**
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  }
}
```

**For CloudAMQP:**
```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://username:password@instance.cloudamqp.com/vhost"
  }
}
```

Update in **BOTH**:
- `SenderApp/appsettings.json`
- `ClientA/appsettings.json`

---

## ?? Testing

### Via Swagger
1. Open https://localhost:5001/swagger
2. Click GET /Order ? Try it out ? Execute
3. Check ClientA console

### Via PowerShell
```powershell
# Send one order
.\test-order.ps1

# Send 5 orders
.\test-order.ps1 -Count 5
```

### Via curl
```bash
curl -X GET https://localhost:5001/Order -k
```

---

## ?? Expected Output

### SenderApp (API Response)
```json
{
  "orderId": "abc123...",
  "status": "Accepted"
}
```

### ClientA (Console)
```
ClientA is running. Press any key to exit...
Connected to: localhost  // or "CloudAMQP"
===========================================
Order Received!
Order ID: abc123...
Details: Sample Order
Placed At: 2024-01-15 10:30:00
===========================================
```

---

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| Connection refused | Check RabbitMQ is running: `docker ps` |
| Messages not received | Verify same connection string in both apps |
| Swagger not showing | Use HTTPS: https://localhost:5001/swagger |
| Build errors | Run `dotnet restore` then `dotnet build` |
| CloudAMQP connection fails | Check URL is complete with `amqps://` prefix |

---

## ?? Cleanup

```powershell
# Stop apps: Press Ctrl+C in each terminal

# Stop RabbitMQ
docker-compose down

# Remove all data
docker-compose down -v
```

---

## ?? Documentation Files

- **README.md** - Main documentation
- **CLOUDAMQP-SETUP.md** - Detailed CloudAMQP guide
- **SUMMARY.md** - Implementation overview
- **QUICK-REFERENCE.md** - This file

---

## ? Most Common Use Cases

### 1. Local Testing
```powershell
docker-compose up -d
cd ClientA && dotnet run    # Terminal 1
cd SenderApp && dotnet run  # Terminal 2
# Open https://localhost:5001/swagger
```

### 2. Demo on Two Computers
1. Set up CloudAMQP
2. Update both `appsettings.json` files
3. Copy folders to each computer
4. Run and test!

### 3. Switch from Local to CloudAMQP
Just change the connection string in both `appsettings.json` files. No code changes needed! ?
