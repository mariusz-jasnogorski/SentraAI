using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SentraAI.Messaging;
using SentraAI.Notifications;
using SentraAI.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSentraAIObservability()
    .AddSentraAINotifications()
    .AddSentraAIMessaging(builder.Configuration)
    .AddHostedService<RecommendationConsumerWorker>();

await builder.Build().RunAsync();
