# SentraAI

A clean .NET 10 LTS solution for an agentic AI Sentra AI MVP.

The solution demonstrates:

- Vera-like event simulation
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
src/SentraAI.Persistence      In-memory store and fake Vera data source
src/SentraAI.Agents           Rule agent, automation agent, LLM agent, tools, policy pipeline
src/SentraAI.Notifications    Communication agent and console notification channel
src/SentraAI.Observability    Local-safe observability configuration adapter
tests/SentraAI.Agents.Tests   Basic policy tests
```

## Production next steps

For real production deployment, replace demo pieces gradually:

1. Replace `FakeVeraDataSource` with a `VeraCollector` using Vera Edge HTTP API.
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
