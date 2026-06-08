using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Integrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAISmartHomeIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmartHomeIntegrationOptions>(configuration.GetSection("SmartHomeIntegration"));
        services.Configure<VeraOptions>(configuration.GetSection("Vera"));

        services.AddHttpClient<VeraSmartHomeEventSource>();
        services.AddSingleton<FakeSmartHomeEventSource>();

        services.AddSingleton<ISmartHomeEventSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SmartHomeIntegrationOptions>>().Value;
            return options.Provider.Equals("Vera", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<VeraSmartHomeEventSource>()
                : sp.GetRequiredService<FakeSmartHomeEventSource>();
        });

        return services;
    }
}
