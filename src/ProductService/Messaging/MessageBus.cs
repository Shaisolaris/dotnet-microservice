namespace ProductService.Messaging;

using System.Text;
using System.Text.Json;
using ProductService.Models;
using RabbitMQ.Client;

public interface IMessageBus
{
    Task PublishAsync(ProductEvent evt);
}

public class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private const string Exchange = "product.events";

    public RabbitMqMessageBus(IConfiguration config, ILogger<RabbitMqMessageBus> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true);
        _logger.LogInformation("RabbitMQ connected to {Host}", factory.HostName);
    }

    public Task PublishAsync(ProductEvent evt)
    {
        var routingKey = $"product.{evt.EventType}";
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(Exchange, routingKey, properties, body);
        _logger.LogInformation("Published {EventType} for product {ProductId}", evt.EventType, evt.ProductId);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

public class InMemoryMessageBus : IMessageBus
{
    private readonly ILogger<InMemoryMessageBus> _logger;
    private readonly List<ProductEvent> _events = new();

    public InMemoryMessageBus(ILogger<InMemoryMessageBus> logger) => _logger = logger;

    public Task PublishAsync(ProductEvent evt)
    {
        _events.Add(evt);
        _logger.LogInformation("[InMemory] Published {EventType} for {ProductId}", evt.EventType, evt.ProductId);
        return Task.CompletedTask;
    }

    public IReadOnlyList<ProductEvent> GetEvents() => _events.AsReadOnly();
}
