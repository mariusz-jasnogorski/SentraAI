namespace SentraAI.Contracts;

public sealed class SmartHomeIntegrationOptions { public string Provider { get; set; } = "Fake"; }
public sealed class VeraOptions { public string BaseUrl { get; set; } = "http://192.168.1.50:3480/"; }
public sealed class EventPublishingOptions
{
    public string Mode { get; set; } = "InProcess";
    public string Endpoint { get; set; } = "http://localhost:5100/api/home-events";
    public string ApiKey { get; set; } = "dev-local-key";
}
public sealed class ConnectorOptions { public int PollingIntervalSeconds { get; set; } = 30; }
public sealed class SentraAIRuntimeOptions { public string RuntimeMode { get; set; } = "LocalAllInOne"; }
public sealed class PolicyOptions
{
    public double MinimumConfidence { get; set; } = 0.55;
    public double CriticalMinimumConfidence { get; set; } = 0.80;
    public int CriticalMinimumEvidenceCount { get; set; } = 2;
}
