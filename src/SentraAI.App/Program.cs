using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentraAI.Agents;
using SentraAI.Contracts;
using SentraAI.Notifications;
using SentraAI.Observability;
using SentraAI.Persistence;

// This application is intentionally a console/worker-style MVP.
// It demonstrates the full architecture locally without requiring Azure, Vera Edge,
// PostgreSQL, RabbitMQ or an LLM API key.

var builder = Host.CreateApplicationBuilder(args);

// Console logging is always enabled for local development.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Application Insights / Azure Monitor is optional.
// If APPLICATIONINSIGHTS_CONNECTION_STRING is not present, the app still runs.
builder.Services.AddSentraAIObservability(builder.Configuration);

// Persistence / context building.
builder.Services.AddSingleton<InMemorySentraAIStore>();
builder.Services.AddSingleton<SentraAIContextBuilder>();
builder.Services.AddSingleton<FakeVeraDataSource>();

// Core agents.
builder.Services.AddSingleton<ISentraAIAgent, RuleAnomalyAgent>();
builder.Services.AddSingleton<ISentraAIAgent, AutomationDiscoveryAgent>();

// LLM agent setup.
// FakeLlmClient is used by default so the solution works without cloud credentials.
builder.Services.Configure<LlmAgentOptions>(options =>
{
    options.MaxSteps = 4;
    options.MinimumConfidence = 0.70;
    options.ToolTimeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddSingleton<ILlmClient, FakeLlmClient>();
builder.Services.AddSingleton<IAgentTool, QueryRecentEventsTool>();
builder.Services.AddSingleton<IAgentTool, RetrieveSimilarSituationsTool>();
builder.Services.AddSingleton<AgentToolRegistry>();
builder.Services.AddSingleton<LlmAnomalyAgent>();

// Register the same LLM agent implementation as a SentraAI agent.
// This small factory avoids having two different LlmAnomalyAgent instances.
builder.Services.AddSingleton<ISentraAIAgent>(sp => sp.GetRequiredService<LlmAnomalyAgent>());

// Policy + recommendation pipeline.
builder.Services.AddSingleton<IPolicyEngine, DefaultPolicyEngine>();
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<SentraAIAgentPipeline>();

// Communication / notifications.
builder.Services.AddSingleton<CommunicationAgent>();
builder.Services.AddSingleton<INotificationChannel, ConsoleNotificationChannel>();
builder.Services.AddSingleton<NotificationRouter>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SentraAI.App");
var store = host.Services.GetRequiredService<InMemorySentraAIStore>();
var fakeVera = host.Services.GetRequiredService<FakeVeraDataSource>();
var contextBuilder = host.Services.GetRequiredService<SentraAIContextBuilder>();
var pipeline = host.Services.GetRequiredService<SentraAIAgentPipeline>();
var communication = host.Services.GetRequiredService<CommunicationAgent>();
var notifications = host.Services.GetRequiredService<NotificationRouter>();

logger.LogInformation("Generating demo Vera-like events...");
store.AddEvents(fakeVera.GenerateDemoEvents());

logger.LogInformation("Building SentraAIContext...");
var context = await contextBuilder.BuildAsync(CancellationToken.None);

logger.LogInformation("Running agent pipeline...");
var recommendations = await pipeline.ProcessAsync(context, CancellationToken.None);

logger.LogInformation("Sending user messages...");
foreach (var recommendation in recommendations)
{
    var message = communication.CreateMessage(recommendation);
    await notifications.RouteAsync(message, CancellationToken.None);
}

logger.LogInformation("Done. Findings: {Findings}, Recommendations: {Recommendations}",
    store.GetFindings().Count,
    store.GetRecommendations().Count);

Console.WriteLine("Press ENTER to exit...");
Console.ReadLine();
