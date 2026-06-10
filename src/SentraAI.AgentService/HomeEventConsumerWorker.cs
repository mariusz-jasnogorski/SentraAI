using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SentraAI.Contracts;
using SentraAI.Messaging;
using System.Text.Json;

namespace SentraAI.AgentService;

public sealed class HomeEventConsumerWorker(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> options,
    ISentraAIStore store,
    ISentraAIContextBuilder contextBuilder,
    ISentraAIAgentPipeline pipeline,
    IRecommendationPublisher recommendationPublisher,
    ILogger<HomeEventConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMq = options.Value;
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        RabbitMqConnectionFactory.DeclareHomeEventsTopology(channel, rabbitMq);
        RabbitMqConnectionFactory.DeclareRecommendationsTopology(channel, rabbitMq);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, delivery) =>
        {
            try
            {
                var events = JsonSerializer.Deserialize<List<HomeEvent>>(delivery.Body.Span, SentraAIJson.SerializerOptions) ?? [];
                logger.LogInformation("Consumed {Count} HomeEvent messages", events.Count);

                if (events.Count > 0)
                {
                    await store.AddEventsAsync(events, stoppingToken);
                    var context = await contextBuilder.BuildAsync(stoppingToken);
                    var recommendations = await pipeline.RunAsync(context, stoppingToken);
                    await recommendationPublisher.PublishAsync(recommendations, stoppingToken);
                }

                channel.BasicAck(delivery.DeliveryTag, multiple: false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process HomeEvent message");
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(rabbitMq.HomeEventsQueue, autoAck: false, consumer);
        logger.LogInformation("AgentService is consuming HomeEvent messages from queue {Queue}", rabbitMq.HomeEventsQueue);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
