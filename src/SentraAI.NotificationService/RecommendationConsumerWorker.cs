using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SentraAI.Contracts;
using SentraAI.Messaging;
using System.Text.Json;

namespace SentraAI.NotificationService;

public sealed class RecommendationConsumerWorker(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> options,
    ICommunicationAgent communicationAgent,
    INotificationChannel notificationChannel,
    ILogger<RecommendationConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMq = options.Value;
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        RabbitMqConnectionFactory.DeclareRecommendationsTopology(channel, rabbitMq);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, delivery) =>
        {
            try
            {
                var recommendations = JsonSerializer.Deserialize<List<Recommendation>>(delivery.Body.Span, SentraAIJson.SerializerOptions) ?? [];
                logger.LogInformation("Consumed {Count} Recommendation messages", recommendations.Count);

                foreach (var recommendation in recommendations)
                {
                    var message = await communicationAgent.CreateMessageAsync(recommendation, stoppingToken);
                    await notificationChannel.SendAsync(message, stoppingToken);
                }

                channel.BasicAck(delivery.DeliveryTag, multiple: false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process Recommendation message");
                channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(rabbitMq.RecommendationsQueue, autoAck: false, consumer);
        logger.LogInformation("NotificationService is consuming Recommendation messages from queue {Queue}", rabbitMq.RecommendationsQueue);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
