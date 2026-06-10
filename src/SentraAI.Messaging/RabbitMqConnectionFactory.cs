using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SentraAI.Contracts;

namespace SentraAI.Messaging;

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
{
    public IConnection CreateConnection()
    {
        var rabbitMq = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = rabbitMq.HostName,
            Port = rabbitMq.Port,
            UserName = rabbitMq.UserName,
            Password = rabbitMq.Password,
            VirtualHost = rabbitMq.VirtualHost,
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection("SentraAI");
    }

    public static void DeclareHomeEventsTopology(IModel channel, RabbitMqOptions options)
    {
        channel.ExchangeDeclare(options.HomeEventsExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(options.HomeEventsQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.HomeEventsQueue, options.HomeEventsExchange, options.HomeEventsRoutingKey);
    }

    public static void DeclareRecommendationsTopology(IModel channel, RabbitMqOptions options)
    {
        channel.ExchangeDeclare(options.RecommendationsExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(options.RecommendationsQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.RecommendationsQueue, options.RecommendationsExchange, options.RecommendationsRoutingKey);
    }
}
