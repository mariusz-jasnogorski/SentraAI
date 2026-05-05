# SentraAI

A clean .NET 10 LTS solution for an agentic AI Sentra AI MVP.

The solution demonstrates:

- Vera-like event simulation
- real Vera Plus / Vera Edge integration through a generic smart home adapter
- normalized Sentra AI events
- in-memory persistence for local demo mode
- rule-based anomaly detection
- automation discovery
- generic tool-based LLM anomaly agent
- fake LLM client for local execution without API keys
- policy-controlled recommendations
- user-facing communication flow
- local-safe observability setup
- central package version management

## Why this ZIP is clean

This version avoids the previous NuGet downgrade issues by using:

- `net10.0` consistently across every project
- `Directory.Build.props` for common project settings
- `Directory.Packages.props` for central package versions
- no mixed `Microsoft.Extensions.*` 8.x and 9.x dependency stack
- no forced Azure Monitor exporter package during local development

Application Insights is configuration-ready, but not forced at startup. The app runs locally even when `APPLICATIONINSIGHTS_CONNECTION_STRING` is missing.

## Run

```bash
dotnet restore
dotnet build
dotnet run --project src/SentraAI.App/SentraAI.App.csproj
```

## Test

```bash
dotnet test
```

## Projects

```text
src/SentraAI.App              Console host / local runner
src/SentraAI.Contracts        Shared domain models and interfaces
src/SentraAI.Persistence      In-memory store
src/SentraAI.Integrations    Generic smart home integration abstraction, Fake source and Vera adapter
src/SentraAI.Agents           Rule agent, automation agent, LLM agent, tools, policy pipeline
src/SentraAI.Notifications    Communication agent and console notification channel
src/SentraAI.Observability    Local-safe observability configuration adapter
tests/SentraAI.Agents.Tests   Basic policy tests
```

## Production next steps

For real production deployment, replace demo pieces gradually:

1. Switch `SmartHomeIntegration:Provider` from `Fake` to `Vera` and set `Vera:BaseUrl`.
2. Replace `InMemorySentraAIStore` with PostgreSQL / EF Core.
3. Replace `FakeLlmClient` with Ollama, OpenAI, or Azure OpenAI.
4. Add RabbitMQ / Azure Service Bus between collector and agent service.
5. Add Azure Monitor / Application Insights exporter after selecting package versions compatible with your .NET target.
6. Add a user interface for accepting/rejecting automation suggestions.

## Safety rule

Agents may reason, recommend, and explain.
Agents must not directly execute physical Sentra AI actions.
Execution should always go through policy validation and user approval.


## .NET 10 upgrade notes

This package targets `net10.0` and uses Microsoft.Extensions `10.0.0` packages consistently.
Install the .NET 10 SDK before building. The included `global.json` requests SDK `10.0.201` and rolls forward to the latest feature band if available.

Application Insights / Azure Monitor remains optional for local runs. If `APPLICATIONINSIGHTS_CONNECTION_STRING` is not set, the application uses console/local telemetry only.

## Smart home integrations

The solution now uses a vendor-neutral interface:

```csharp
ISmartHomeEventSource
```

The agent pipeline receives normalized `HomeEvent` records and does not know whether the data came from Vera, a simulator, or a future platform. This keeps the design open for Home Assistant, Fibaro, MQTT, KNX or another smart home system.

### Local simulator mode

This is the default mode and requires no hardware:

```json
{
  "SmartHomeIntegration": {
    "Provider": "Fake"
  }
}
```

### Real Vera Plus / Vera Edge mode

Set the provider to `Vera` and point `BaseUrl` to the local Vera controller address:

```json
{
  "SmartHomeIntegration": {
    "Provider": "Vera"
  },
  "Vera": {
    "BaseUrl": "http://192.168.1.50:3480/"
  }
}
```

The Vera adapter calls:

```text
/data_request?id=sdata&output_format=json
```

and maps Vera devices into normalized SentraAI `HomeEvent` records.

### Future integrations

To add another smart home system, create a new class that implements:

```csharp
public interface ISmartHomeEventSource
{
    Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken);
}
```

Then register it in `AddSentraAISmartHomeIntegration`. The agents, policies, recommendations and notifications do not need to change.
