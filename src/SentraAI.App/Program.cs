using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentraAI.Agents;
using SentraAI.Contracts;
using SentraAI.Integrations;
using SentraAI.Notifications;
using SentraAI.Observability;
using SentraAI.Persistence;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services
    .AddSentraAIObservability()
    .AddSentraAIPersistence()
    .AddSentraAISmartHomeIntegration(builder.Configuration)
    .AddSentraAINotifications()
    .AddSentraAIAgents(builder.Configuration);

using var host = builder.Build();
var orchestrator = host.Services.GetRequiredService<ISentraAIOrchestrator>();
await orchestrator.RunOnceAsync(CancellationToken.None);
