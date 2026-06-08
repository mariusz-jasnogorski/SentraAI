using Microsoft.Extensions.DependencyInjection;
using SentraAI.Contracts;

namespace SentraAI.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAIPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ISentraAIStore, InMemorySentraAIStore>();
        services.AddSingleton<ISentraAIContextBuilder, SentraAIContextBuilder>();
        return services;
    }
}
