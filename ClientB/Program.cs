using Messages;
using NServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClientB.Services;

Console.Title = "ClientB";

// Build configuration to read appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var endpointConfiguration = new EndpointConfiguration("ClientB");

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

// No routing needed - subscribing to events, not handling commands

// Disable immediate and delayed retries - we'll use Polly instead
var recoverability = endpointConfiguration.Recoverability();
recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
recoverability.Delayed(delayed => delayed.NumberOfRetries(0));

endpointConfiguration.SendFailedMessagesTo("error");
endpointConfiguration.EnableInstallers();

// Register Polly policies in the container
endpointConfiguration.RegisterComponents(services =>
{
    services.AddSingleton(sp => new PollyPolicies(configuration));
});

var endpointInstance = await NServiceBus.Endpoint.Start(endpointConfiguration);

var connectionType = rabbitMqConnectionString.Contains("cloudamqp", StringComparison.OrdinalIgnoreCase) 
    ? "CloudAMQP" 
    : "localhost";

Console.WriteLine("===========================================");
Console.WriteLine("ClientB is running and listening for events...");
Console.WriteLine($"Connected to: {connectionType}");
Console.WriteLine("Subscribed to: OrderPlaced events");
Console.WriteLine("Polly Policies:");
Console.WriteLine($"  - Retry: {configuration["Polly:RetryPolicy:MaxRetryAttempts"]} attempts with {configuration["Polly:RetryPolicy:DelayBetweenRetriesSeconds"]}s delay");
Console.WriteLine($"  - Circuit Breaker: {configuration["Polly:CircuitBreaker:FailureThreshold"]} failures, {configuration["Polly:CircuitBreaker:DurationOfBreakSeconds"]}s break");
Console.WriteLine("===========================================");
Console.WriteLine("Test with different ExceptionType values:");
Console.WriteLine("  - 'retry' -> Triggers RetryableException (Polly will retry 3 times)");
Console.WriteLine("  - 'circuit-breaker' -> Triggers CircuitBreakerException (opens circuit)");
Console.WriteLine("  - null or empty -> Success");
Console.WriteLine("===========================================");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

await endpointInstance.Stop();
