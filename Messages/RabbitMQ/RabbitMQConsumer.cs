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

    public RabbitMQConsumer(RabbitMQConnection rabbitMQConnection)
    {
        _rabbitMQConnection = rabbitMQConnection ?? throw new ArgumentNullException(nameof(rabbitMQConnection));
    }

    public void Initialize(string queueName, string[] routingKeys)
    {
        _queueName = queueName;
        var connection = _rabbitMQConnection.GetConnection();
        _channel = connection.CreateModel();

        // Declare the exchange
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare the queue as quorum queue (matching NServiceBus configuration)
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

        // Bind queue to exchange with routing keys
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
                        // Reject and don't requeue (send to DLQ if configured)
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                // Reject and don't requeue (send to DLQ if configured)
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}
