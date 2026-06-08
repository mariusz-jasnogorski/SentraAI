using Microsoft.Extensions.Options;
using SentraAI.Contracts;
using System.Net.Http.Json;

namespace SentraAI.Agents;

public sealed class InProcessHomeEventPublisher(ISentraAIOrchestrator orchestrator) : IHomeEventPublisher
{
    public async Task PublishAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken)
        => await orchestrator.ProcessEventsAsync(events, cancellationToken);
}

public sealed class HttpHomeEventPublisher(HttpClient httpClient, IOptions<EventPublishingOptions> options) : IHomeEventPublisher
{
    public async Task PublishAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.Endpoint)
        {
            Content = JsonContent.Create(events)
        };

        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            request.Headers.Add("X-SentraAI-ApiKey", options.Value.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
