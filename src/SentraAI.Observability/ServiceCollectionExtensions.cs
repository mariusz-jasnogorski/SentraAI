using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SentraAI.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAIObservability(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        }));

        return services;
    }
}
