using Messages;
using Messages.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClientB.Services;
using ClientB.Handlers;

Console.Title = "ClientB";

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

// Setup Dependency Injection
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<PollyPolicies>();
services.AddSingleton<InboxService>(sp => new InboxService("clientb-inbox.txt"));
services.AddSingleton<RabbitMQConnection>(sp => new RabbitMQConnection(rabbitMqConnectionString));
services.AddSingleton<OrderPlacedHandler>();

var serviceProvider = services.BuildServiceProvider();

var rabbitMQConnection = serviceProvider.GetRequiredService<RabbitMQConnection>();
using var consumer = new RabbitMQConsumer(rabbitMQConnection);

consumer.Initialize("ClientB", new[] { "order.placed" });

var connectionType = rabbitMqConnectionString.Contains("cloudamqp", StringComparison.OrdinalIgnoreCase) 
    ? "CloudAMQP" 
    : "localhost";

Console.WriteLine("===========================================");
Console.WriteLine("ClientB is running and listening for events...");
Console.WriteLine($"Connected to: {connectionType}");
Console.WriteLine("Queue: ClientB");
Console.WriteLine("Subscribed to: order.placed events");
Console.WriteLine("Inbox Pattern: Enabled (clientb-inbox.txt)");
Console.WriteLine("Polly Policies:");
Console.WriteLine($"  - Retry: {configuration["Polly:RetryPolicy:MaxRetryAttempts"]} attempts with {configuration["Polly:RetryPolicy:DelayBetweenRetriesSeconds"]}s delay");
Console.WriteLine($"  - Circuit Breaker: {configuration["Polly:CircuitBreaker:FailureThreshold"]} failures, {configuration["Polly:CircuitBreaker:DurationOfBreakSeconds"]}s break");
Console.WriteLine("===========================================");
Console.WriteLine("Test with different ExceptionType values:");
Console.WriteLine("  - 'retry' -> Triggers RetryableException (Polly will retry 3 times)");
Console.WriteLine("  - 'circuit-breaker' -> Triggers CircuitBreakerException (opens circuit)");
Console.WriteLine("  - 'fatal' -> Triggers FatalException (fails immediately, no resilience)");
Console.WriteLine("  - null or empty -> Success");
Console.WriteLine("===========================================");
Console.WriteLine("Press any key to exit...");

var handler = serviceProvider.GetRequiredService<OrderPlacedHandler>();

consumer.StartConsuming<OrderPlaced>(async message =>
{
    return await handler.HandleAsync(message);
});

Console.ReadKey();
