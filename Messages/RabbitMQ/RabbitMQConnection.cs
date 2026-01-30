using RabbitMQ.Client;

namespace Messages.RabbitMQ;

public class RabbitMQConnection : IDisposable
{
    private IConnection? _connection;
    private readonly string _connectionString;

    public RabbitMQConnection(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IConnection GetConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            var factory = ParseConnectionString(_connectionString);
            _connection = factory.CreateConnection();
        }
        return _connection;
    }

    private ConnectionFactory ParseConnectionString(string connectionString)
    {
        var factory = new ConnectionFactory();

        // Check if it's CloudAMQP format (amqps://)
        if (connectionString.StartsWith("amqps://", StringComparison.OrdinalIgnoreCase))
        {
            factory.Uri = new Uri(connectionString);
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
        }
        else
        {
            // Parse localhost format: host=localhost;username=guest;password=guest
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2) continue;

                var key = keyValue[0].Trim().ToLower();
                var value = keyValue[1].Trim();

                switch (key)
                {
                    case "host":
                        factory.HostName = value;
                        break;
                    case "username":
                        factory.UserName = value;
                        break;
                    case "password":
                        factory.Password = value;
                        break;
                    case "port":
                        factory.Port = int.Parse(value);
                        break;
                }
            }

            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
        }

        return factory;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
