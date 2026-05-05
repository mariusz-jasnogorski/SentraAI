using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentraAI.Contracts;

namespace SentraAI.Integrations;

/// <summary>
/// Registers the selected smart home integration.
///
/// Important design point:
/// The app asks for ISmartHomeEventSource, not VeraEventSource directly.
/// That keeps the core pipeline open for future providers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAISmartHomeIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmartHomeIntegrationOptions>(configuration.GetSection("SmartHomeIntegration"));
        services.Configure<VeraOptions>(configuration.GetSection("Vera"));

        var provider = configuration["SmartHomeIntegration:Provider"] ?? "Fake";

        if (provider.Equals("Vera", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<VeraEventMapper>();
            services.AddHttpClient<VeraClient>();
            services.AddSingleton<ISmartHomeEventSource, VeraEventSource>();
        }
        else
        {
            services.AddSingleton<ISmartHomeEventSource, FakeSmartHomeEventSource>();
        }

        return services;
    }
}
