using RabbitMQ.Client;

namespace Warehouse.Messaging;

public sealed class RabbitOptions
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "warehouse";

    public string Password { get; set; } = "warehouse_password";

    public string VirtualHost { get; set; } = "/";

    public string[] Endpoints { get; set; } = [];

    public ConnectionFactory CreateFactory()
    {
        var endpoints = Endpoints.Length == 0
            ? new[]
            {
                new AmqpTcpEndpoint(HostName, Port)
            }
            : Endpoints.Select(ParseEndpoint).ToArray();

        return new ConnectionFactory
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            VirtualHost = VirtualHost,

            RequestedHeartbeat = TimeSpan.FromSeconds(10),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),

            // Existing publisher and subscriber loops handle reconnection.
            AutomaticRecoveryEnabled = false,

            // Existing CreateConnectionAsync calls now try this endpoint list.
            EndpointResolverFactory =
                _ => new DefaultEndpointResolver(endpoints)
        };
    }

    private static AmqpTcpEndpoint ParseEndpoint(string value)
    {
        var uri = new Uri($"amqp://{value}");

        return new AmqpTcpEndpoint(
            uri.Host,
            uri.Port > 0 ? uri.Port : 5672);
    }
}