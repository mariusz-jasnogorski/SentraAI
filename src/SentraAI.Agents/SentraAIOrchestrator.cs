using Microsoft.Extensions.Logging;
using SentraAI.Contracts;

namespace SentraAI.Agents;

public sealed class SentraAIOrchestrator(
    ISmartHomeEventSource eventSource,
    ISentraAIStore store,
    ISentraAIContextBuilder contextBuilder,
    ISentraAIAgentPipeline pipeline,
    ICommunicationAgent communicationAgent,
    INotificationChannel notificationChannel,
    ILogger<SentraAIOrchestrator> logger) : ISentraAIOrchestrator
{
    public async Task<IReadOnlyList<Recommendation>> RunOnceAsync(CancellationToken cancellationToken)
    {
        var events = await eventSource.ReadEventsAsync(cancellationToken);
        return await ProcessEventsAsync(events, cancellationToken);
    }

    public async Task<IReadOnlyList<Recommendation>> ProcessEventsAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing {Count} smart-home events", events.Count);
        await store.AddEventsAsync(events, cancellationToken);
        var context = await contextBuilder.BuildAsync(cancellationToken);
        var recommendations = await pipeline.RunAsync(context, cancellationToken);

        foreach (var recommendation in recommendations)
        {
            var message = await communicationAgent.CreateMessageAsync(recommendation, cancellationToken);
            await notificationChannel.SendAsync(message, cancellationToken);
        }

        return recommendations;
    }
}
