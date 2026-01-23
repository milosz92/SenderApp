# appsettings.json Configuration Examples

## For CloudAMQP (Different Computers)

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqps://abcdefgh:xYz123-AbC456@hawk.rmq.cloudamqp.com/abcdefgh"
  }
}
```

Replace with your actual CloudAMQP URL from https://www.cloudamqp.com/

---

## For Local Docker RabbitMQ (Same Computer)

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  }
}
```

Make sure to run `docker-compose up -d` first!

---

## Where to Update

You need to update **BOTH** files with the **SAME** connection string:

1. `SenderApp/appsettings.json`
2. `ClientA/appsettings.json`

---

## Quick Switch

**From CloudAMQP to Local:**
1. Run `docker-compose up -d`
2. Change `"RabbitMQ"` value in both appsettings.json to: `"host=localhost;username=guest;password=guest"`

**From Local to CloudAMQP:**
1. Get your CloudAMQP URL from dashboard
2. Change `"RabbitMQ"` value in both appsettings.json to your CloudAMQP URL
3. (Can stop docker: `docker-compose down`)
