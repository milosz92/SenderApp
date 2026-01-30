using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Messages.RabbitMQ;

public class RabbitMQPublisher : IDisposable
{
    private readonly RabbitMQConnection _rabbitMQConnection;
    private IModel? _channel;
    private const string ExchangeName = "events";

    public RabbitMQPublisher(RabbitMQConnection rabbitMQConnection)
    {
        _rabbitMQConnection = rabbitMQConnection ?? throw new ArgumentNullException(nameof(rabbitMQConnection));
    }

    public void Initialize()
    {
        var connection = _rabbitMQConnection.GetConnection();
        _channel = connection.CreateModel();

        // Declare a topic exchange for events
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    public void Publish<T>(T message, string routingKey) where T : class
    {
        if (_channel == null)
            throw new InvalidOperationException("Publisher not initialized. Call Initialize first.");

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}
