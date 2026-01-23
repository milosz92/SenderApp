using NServiceBus;
using Messages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var endpointConfiguration = new EndpointConfiguration("SenderApp");

// Configure serialization (using built-in System.Text.Json serializer in NServiceBus 9+)
endpointConfiguration.UseSerialization<SystemJsonSerializer>();

var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();

// Get RabbitMQ connection string from appsettings.json
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");

if (string.IsNullOrEmpty(rabbitMqConnectionString))
{
    throw new InvalidOperationException(
        "RabbitMQ connection string is not configured. " +
        "Please update 'ConnectionStrings:RabbitMQ' in appsettings.json");
}

transport.ConnectionString(rabbitMqConnectionString);
transport.UseConventionalRoutingTopology(QueueType.Quorum);

// No routing needed - events are published, not sent

endpointConfiguration.SendFailedMessagesTo("error");
endpointConfiguration.EnableInstallers();

var endpointInstance = await NServiceBus.Endpoint.Start(endpointConfiguration);

builder.Services.AddSingleton<IMessageSession>(endpointInstance);
builder.Services.AddHostedService(provider => new NServiceBusHostedService(endpointInstance));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("===========================================");
Console.WriteLine("SenderApp is running");
Console.WriteLine("Swagger: https://localhost:5001/swagger");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine("Endpoints:");
Console.WriteLine("  GET  /Order - Publish event to ClientA (choreography)");
Console.WriteLine("  POST /OrderProcess/success - Call ClientB (orchestration)");
Console.WriteLine("  POST /OrderProcess/retry - Call ClientB with retry");
Console.WriteLine("  POST /OrderProcess/circuit-breaker - Call ClientB with circuit breaker");
Console.WriteLine("===========================================");

app.Run();

public class NServiceBusHostedService : IHostedService
{
    private readonly IEndpointInstance _endpointInstance;

    public NServiceBusHostedService(IEndpointInstance endpointInstance)
    {
        _endpointInstance = endpointInstance;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => _endpointInstance.Stop();
}
