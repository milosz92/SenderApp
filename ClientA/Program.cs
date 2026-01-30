using Messages;
using Messages.RabbitMQ;
using Microsoft.Extensions.Configuration;
using ClientA.Handlers;

Console.Title = "ClientA";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ");

if (string.IsNullOrEmpty(rabbitMqConnectionString))
{
    Console.WriteLine("ERROR: RabbitMQ connection string is not configured.");
    Console.WriteLine("Please update 'ConnectionStrings:RabbitMQ' in appsettings.json");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

using var rabbitMQConnection = new RabbitMQConnection(rabbitMqConnectionString);
using var consumer = new RabbitMQConsumer(rabbitMQConnection);

consumer.Initialize("ClientA", new[] { "order.placed" });

var connectionType = rabbitMqConnectionString.Contains("cloudamqp", StringComparison.OrdinalIgnoreCase) 
    ? "CloudAMQP" 
    : "localhost";

Console.WriteLine("===========================================");
Console.WriteLine("ClientA is running and listening for messages...");
Console.WriteLine($"Connected to: {connectionType}");
Console.WriteLine("Queue: ClientA");
Console.WriteLine("Subscribed to: order.placed events");
Console.WriteLine("Press any key to exit...");
Console.WriteLine("===========================================");

var handler = new OrderPlacedHandler();

consumer.StartConsuming<OrderPlaced>(async message => await handler.HandleAsync(message));

Console.ReadKey();
