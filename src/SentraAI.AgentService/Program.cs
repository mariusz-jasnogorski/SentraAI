using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentraAI.Agents;
using SentraAI.Contracts;
using SentraAI.Messaging;
using SentraAI.Notifications;
using SentraAI.Observability;
using SentraAI.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ISmartHomeEventSource, EmptySmartHomeEventSource>();

builder.Services
    .AddSentraAIObservability()
    .AddSentraAIPersistence()
    .AddSentraAINotifications()
    .AddSentraAIAgents(builder.Configuration)
    .AddSentraAIMessaging(builder.Configuration)
    .AddHostedService<HomeEventConsumerWorker>();

await builder.Build().RunAsync();

public sealed class EmptySmartHomeEventSource : ISmartHomeEventSource
{
    public Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<HomeEvent>>([]);
}
