using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSentraAIAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PolicyOptions>(configuration.GetSection("Policy"));
        services.Configure<EventPublishingOptions>(configuration.GetSection("EventPublishing"));

        services.AddSingleton<ISentraAIAgent, RuleAnomalyAgent>();
        services.AddSingleton<ISentraAIAgent, AutomationDiscoveryAgent>();
        services.AddSingleton<ISentraAIAgent, LlmAnomalyAgent>();
        services.AddSingleton<ILlmClient, FakeLlmClient>();
        services.AddSingleton<IAgentTool, QueryRecentEventsTool>();
        services.AddSingleton<IAgentTool, QueryDeviceStatesTool>();
        services.AddSingleton<IPolicyEngine, DefaultPolicyEngine>();
        services.AddSingleton<IRecommendationService, RecommendationService>();
        services.AddSingleton<ISentraAIAgentPipeline, SentraAIAgentPipeline>();
        services.AddSingleton<ISentraAIOrchestrator, SentraAIOrchestrator>();

        services.AddSingleton<HttpHomeEventPublisher>();
        services.AddSingleton<InProcessHomeEventPublisher>();
        services.AddSingleton<IHomeEventPublisher>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EventPublishingOptions>>().Value;
            return options.Mode.Equals("Http", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<HttpHomeEventPublisher>()
                : sp.GetRequiredService<InProcessHomeEventPublisher>();
        });

        return services;
    }
}
