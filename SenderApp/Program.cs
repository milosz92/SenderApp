using Messages.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get RabbitMQ connection string
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");

if (string.IsNullOrEmpty(rabbitMqConnectionString))
{
    throw new InvalidOperationException(
        "RabbitMQ connection string is not configured. " +
        "Please update 'ConnectionStrings:RabbitMQ' in appsettings.json");
}

// Register RabbitMQ services
var rabbitMQConnection = new RabbitMQConnection(rabbitMqConnectionString);
builder.Services.AddSingleton(rabbitMQConnection);

var rabbitMQPublisher = new RabbitMQPublisher(rabbitMQConnection);
rabbitMQPublisher.Initialize();
builder.Services.AddSingleton(rabbitMQPublisher);

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

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine("===========================================");
Console.WriteLine("SenderApp is running");
Console.WriteLine("Swagger: https://localhost:5001/swagger");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine("Endpoints:");
Console.WriteLine("  GET  /Order - Publish OrderPlaced event");
Console.WriteLine("  POST /Order/process - Publish OrderPlaced with exception scenarios");
Console.WriteLine("===========================================");

app.Run();
