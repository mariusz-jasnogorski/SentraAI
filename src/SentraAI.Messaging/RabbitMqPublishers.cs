using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SentraAI.Contracts;
using System.Net.Http.Json;
using System.Text.Json;

namespace SentraAI.Messaging;

public sealed class HttpHomeEventPublisher(HttpClient httpClient, IOptions<EventPublishingOptions> options) : IHomeEventPublisher
{
    public async Task PublishAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.Endpoint)
        {
            Content = JsonContent.Create(events, options: SentraAIJson.SerializerOptions)
        };

        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            request.Headers.Add("X-SentraAI-ApiKey", options.Value.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class RabbitMqHomeEventPublisher : IHomeEventPublisher, IDisposable
{
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<RabbitMqHomeEventPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private bool _disposed;

    public RabbitMqHomeEventPublisher(RabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<RabbitMqHomeEventPublisher> logger)
    {
        _options = options;
        _logger = logger;
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task PublishAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0) return Task.CompletedTask;

        var rabbitMq = _options.Value;
        RabbitMqConnectionFactory.DeclareHomeEventsTopology(_channel, rabbitMq);

        var body = JsonSerializer.SerializeToUtf8Bytes(events, SentraAIJson.SerializerOptions);
        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.MessageId = Guid.NewGuid().ToString("N");
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Type = nameof(HomeEvent);

        _channel.BasicPublish(rabbitMq.HomeEventsExchange, rabbitMq.HomeEventsRoutingKey, mandatory: false, basicProperties: properties, body: body);
        _logger.LogInformation("Published {Count} HomeEvent messages to RabbitMQ exchange {Exchange}", events.Count, rabbitMq.HomeEventsExchange);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.Dispose();
        _connection.Dispose();
    }
}

public sealed class RabbitMqRecommendationPublisher : IRecommendationPublisher, IDisposable
{
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<RabbitMqRecommendationPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private bool _disposed;

    public RabbitMqRecommendationPublisher(RabbitMqConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options, ILogger<RabbitMqRecommendationPublisher> logger)
    {
        _options = options;
        _logger = logger;
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task PublishAsync(IReadOnlyList<Recommendation> recommendations, CancellationToken cancellationToken)
    {
        if (recommendations.Count == 0) return Task.CompletedTask;

        var rabbitMq = _options.Value;
        RabbitMqConnectionFactory.DeclareRecommendationsTopology(_channel, rabbitMq);

        var body = JsonSerializer.SerializeToUtf8Bytes(recommendations, SentraAIJson.SerializerOptions);
        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.MessageId = Guid.NewGuid().ToString("N");
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Type = nameof(Recommendation);

        _channel.BasicPublish(rabbitMq.RecommendationsExchange, rabbitMq.RecommendationsRoutingKey, mandatory: false, basicProperties: properties, body: body);
        _logger.LogInformation("Published {Count} Recommendation messages to RabbitMQ exchange {Exchange}", recommendations.Count, rabbitMq.RecommendationsExchange);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.Dispose();
        _connection.Dispose();
    }
}
