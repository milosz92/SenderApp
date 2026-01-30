using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messages.RabbitMQ;

public class RabbitMQConsumer : IDisposable
{
    private readonly RabbitMQConnection _rabbitMQConnection;
    private IModel? _channel;
    private string? _queueName;
    private const string ExchangeName = "events";
    private const string ErrorQueueName = "error";

    public RabbitMQConsumer(RabbitMQConnection rabbitMQConnection)
    {
        _rabbitMQConnection = rabbitMQConnection ?? throw new ArgumentNullException(nameof(rabbitMQConnection));
    }

    public void Initialize(string queueName, string[] routingKeys)
    {
        _queueName = queueName;
        
        var connection = _rabbitMQConnection.GetConnection();
        _channel = connection.CreateModel();

        // Declare the main exchange
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare the main queue as quorum queue
        var queueArguments = new Dictionary<string, object>
        {
            { "x-queue-type", "quorum" }
        };

        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments);

        // Bind main queue to main exchange with routing keys
        foreach (var routingKey in routingKeys)
        {
            _channel.QueueBind(
                queue: queueName,
                exchange: ExchangeName,
                routingKey: routingKey);
        }

        // Set prefetch count for better load distribution
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    public void StartConsuming<T>(Func<T, Task<bool>> messageHandler) where T : class
    {
        if (_channel == null || _queueName == null)
            throw new InvalidOperationException("Consumer not initialized. Call Initialize first.");

        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            
            try
            {
                var message = JsonSerializer.Deserialize<T>(json);
                if (message != null)
                {
                    var success = await messageHandler(message);
                    
                    if (success)
                    {
                        // Acknowledge the message
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        // Handler returned false - move to error queue
                        MoveToErrorQueue(ea, body, "Handler returned false");
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                Console.WriteLine($"Moving message to error queue: {ErrorQueueName}");
                
                // Move to error queue immediately - Polly handles retries
                MoveToErrorQueue(ea, body, ex.Message, ex.GetType().Name);
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);
    }

    private void MoveToErrorQueue(BasicDeliverEventArgs ea, byte[] body, string errorMessage, string? exceptionType = null)
    {
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>
        {
            ["x-original-queue"] = _queueName!,
            ["x-error-message"] = errorMessage,
            ["x-failed-at"] = DateTime.UtcNow.ToString("O"),
            ["x-original-routing-key"] = ea.RoutingKey
        };

        if (exceptionType != null)
        {
            properties.Headers["x-exception-type"] = exceptionType;
        }

        if (ea.BasicProperties.ContentType != null)
            properties.ContentType = ea.BasicProperties.ContentType;
        if (ea.BasicProperties.ContentEncoding != null)
            properties.ContentEncoding = ea.BasicProperties.ContentEncoding;

        Console.WriteLine($"Moving message to error queue: {ErrorQueueName}");
        Console.WriteLine($"Error: {errorMessage}");

        // Publish directly to error queue using default exchange
        _channel.BasicPublish(
            exchange: "",
            routingKey: ErrorQueueName,
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}
