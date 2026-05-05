using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SentraAI.Observability;

/// <summary>
/// Local-safe observability registration.
///
/// Why this class is intentionally lightweight:
/// - The sample solution must compile cleanly on .NET 10 without package downgrades.
/// - Azure Monitor / Application Insights should be optional for local development.
/// - In production, you can add Azure.Monitor.OpenTelemetry.AspNetCore using versions
///   aligned with your target framework and organization standards.
///
/// The rest of the system depends only on this extension method, not on a specific
/// telemetry provider. That keeps observability replaceable.
/// </summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddSentraAIObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read the connection string if it exists. The local demo does not require it.
        // In Azure Container Apps this value would normally be injected as an environment variable:
        // APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...;IngestionEndpoint=...
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        services.AddSingleton(new SentraAIObservabilityOptions(
            ApplicationInsightsEnabled: !string.IsNullOrWhiteSpace(connectionString),
            ApplicationInsightsConnectionString: connectionString));

        return services;
    }
}

/// <summary>
/// Small options object exposing whether Application Insights configuration is present.
/// This avoids forcing the Azure Monitor exporter during local runs.
/// </summary>
public sealed record SentraAIObservabilityOptions(
    bool ApplicationInsightsEnabled,
    string? ApplicationInsightsConnectionString);
