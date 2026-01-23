using Messages;
using NServiceBus;
using Microsoft.Extensions.Configuration;

Console.Title = "ClientA";

// Build configuration to read appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var endpointConfiguration = new EndpointConfiguration("ClientA");

// Configure serialization
endpointConfiguration.UseSerialization<SystemJsonSerializer>();

var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();

// Get RabbitMQ connection string from appsettings.json
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ");

if (string.IsNullOrEmpty(rabbitMqConnectionString))
{
    Console.WriteLine("ERROR: RabbitMQ connection string is not configured.");
    Console.WriteLine("Please update 'ConnectionStrings:RabbitMQ' in appsettings.json");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

transport.ConnectionString(rabbitMqConnectionString);
transport.UseConventionalRoutingTopology(QueueType.Quorum);

//// Configure error queue
//endpointConfiguration.SendFailedMessagesTo("error");

//// Configure recoverability (retry policy)
//var recoverability = endpointConfiguration.Recoverability();

//// Immediate retries: 3 attempts with no delay
//recoverability.Immediate(
//    immediate =>
//    {
//        immediate.NumberOfRetries(3);
//    });

//// Delayed retries: 2 attempts with increasing delays
//recoverability.Delayed(
//    delayed =>
//    {
//        delayed.NumberOfRetries(2);
//        delayed.TimeIncrease(TimeSpan.FromSeconds(10));
//    });

//endpointConfiguration.EnableInstallers();

var endpointInstance = await Endpoint.Start(endpointConfiguration);

var connectionType = rabbitMqConnectionString.Contains("cloudamqp", StringComparison.OrdinalIgnoreCase) 
    ? "CloudAMQP" 
    : "localhost";

Console.WriteLine("===========================================");
Console.WriteLine("ClientA is running and listening for messages...");
Console.WriteLine($"Connected to: {connectionType}");
Console.WriteLine("Retry Policy: 3 immediate retries + 2 delayed retries");
Console.WriteLine("Error Queue: 'error'");
Console.WriteLine("Press any key to exit...");
Console.WriteLine("===========================================");
Console.ReadKey();

await endpointInstance.Stop();
