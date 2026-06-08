using Microsoft.AspNetCore.Mvc;
using SentraAI.Agents;
using SentraAI.Contracts;
using SentraAI.Notifications;
using SentraAI.Observability;
using SentraAI.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddSentraAIObservability()
    .AddSentraAIPersistence()
    .AddSentraAINotifications()
    .AddSentraAIAgents(builder.Configuration);

// Cloud/API side receives already-normalized HomeEvent instances from the local connector.
// It intentionally does not need direct network access to the smart-home controller.
builder.Services.AddSingleton<ISmartHomeEventSource, EmptySmartHomeEventSource>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SentraAI.Api" }));

app.MapPost("/api/home-events", async (
    [FromBody] IReadOnlyList<HomeEvent> events,
    HttpRequest request,
    ISentraAIOrchestrator orchestrator,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var expectedKey = configuration["EventPublishing:ApiKey"];
    if (!string.IsNullOrWhiteSpace(expectedKey))
    {
        if (!request.Headers.TryGetValue("X-SentraAI-ApiKey", out var actualKey) || actualKey != expectedKey)
            return Results.Unauthorized();
    }

    var recommendations = await orchestrator.ProcessEventsAsync(events, cancellationToken);
    return Results.Ok(new { acceptedEvents = events.Count, recommendations = recommendations.Count });
});

app.Run();

public sealed class EmptySmartHomeEventSource : ISmartHomeEventSource
{
    public Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<HomeEvent>>([]);
}
