using Microsoft.Extensions.DependencyInjection;
using SentraAI.Contracts;

namespace SentraAI.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAINotifications(this IServiceCollection services)
    {
        services.AddSingleton<ICommunicationAgent, CommunicationAgent>();
        services.AddSingleton<INotificationChannel, ConsoleNotificationChannel>();
        return services;
    }
}
