using SentraAI.Agents;
using SentraAI.Connector.Worker;
using SentraAI.Contracts;
using SentraAI.Integrations;
using SentraAI.Observability;
using SentraAI.Persistence;
using SentraAI.Notifications;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<ConnectorOptions>(builder.Configuration.GetSection("Connector"));

builder.Services
    .AddSentraAIObservability()
    .AddSentraAIPersistence()
    .AddSentraAISmartHomeIntegration(builder.Configuration)
    .AddSentraAINotifications()
    .AddSentraAIAgents(builder.Configuration)
    .AddHostedService<ConnectorWorker>();

await builder.Build().RunAsync();