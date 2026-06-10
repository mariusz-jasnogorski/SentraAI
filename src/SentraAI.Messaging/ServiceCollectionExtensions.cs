using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAIMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EventPublishingOptions>(configuration.GetSection("EventPublishing"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddHttpClient<HttpHomeEventPublisher>();
        services.AddSingleton<RabbitMqHomeEventPublisher>();
        services.AddSingleton<RabbitMqRecommendationPublisher>();

        services.AddSingleton<IHomeEventPublisher>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EventPublishingOptions>>().Value;
            return options.Mode.Equals("RabbitMq", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<RabbitMqHomeEventPublisher>()
                : sp.GetRequiredService<HttpHomeEventPublisher>();
        });

        services.AddSingleton<IRecommendationPublisher, RabbitMqRecommendationPublisher>();
        return services;
    }
}
