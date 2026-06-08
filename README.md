# SentraAI

Clean .NET 10 solution for an agentic smart-home AI MVP.

This generated version is prepared for two deployment styles:

1. **Local all-in-one**: `SentraAI.App` runs in the same network as the smart-home controller.
2. **Azure/Core + local Connector**: `SentraAI.Api` can run in Azure, while `SentraAI.Connector.Worker` runs locally in the LAN/VPN where Vera/Home Assistant/Fibaro/MQTT/KNX controller is reachable.

## Key architecture rule

The connector must run in the network that can reach the smart-home controller. The cloud/core side should not need direct access to `192.168.x.x` controller addresses.

```text
Local LAN/VPN near controller
  SentraAI.Connector.Worker
    -> reads Vera/Home Assistant/MQTT/etc.
    -> normalizes to HomeEvent
    -> publishes events through IHomeEventPublisher

Azure or local machine
  SentraAI.Api / SentraAI.App
    -> receives HomeEvent
    -> persists events
    -> builds context
    -> runs agents
    -> applies policy
    -> creates recommendations
    -> sends notifications
```

## Projects

```text
src/SentraAI.App                 Local all-in-one runner
src/SentraAI.Api                 HTTP API for cloud/local core event ingestion
src/SentraAI.Connector.Worker    Local connector that runs near the controller
src/SentraAI.Contracts           Domain models, options and abstractions
src/SentraAI.Persistence         In-memory ISentraAIStore implementation
src/SentraAI.Integrations        Fake and Vera smart-home event sources
src/SentraAI.Agents              Agents, tools, policy, pipeline, orchestrator, publishers
src/SentraAI.Notifications       User communication and console notification channel
src/SentraAI.Observability       Local-safe logging setup
tests/SentraAI.Agents.Tests      Basic behavior tests
```

## Main changes

- Added `ISentraAIStore`; agents and pipeline no longer depend on `InMemorySentraAIStore`.
- Changed agent contract to return many findings: `Task<IReadOnlyList<AgentFinding>>`.
- Added `SentraAIOrchestrator`; `Program.cs` is now only startup/composition code.
- Added `IHomeEventPublisher`, `InProcessHomeEventPublisher`, and `HttpHomeEventPublisher`.
- Added `SentraAI.Api` with `POST /api/home-events`.
- Added `SentraAI.Connector.Worker`, intended to run in the same LAN/VPN as the smart-home controller.
- Added Dockerfiles and `docker-compose.local.yml`.
- Agents may recommend; they do not execute physical smart-home actions.

## Run local all-in-one

```bash
dotnet restore
dotnet build
dotnet run --project src/SentraAI.App/SentraAI.App.csproj
```

## Run API locally

```bash
dotnet run --project src/SentraAI.Api/SentraAI.Api.csproj --urls http://localhost:5100
```

Health check:

```bash
curl http://localhost:5100/health
```

## Run connector locally

The connector should be started on a machine that has network access to the smart-home controller.

```bash
dotnet run --project src/SentraAI.Connector.Worker/SentraAI.Connector.Worker.csproj
```

For Azure, set environment variables on the connector machine:

```bash
EventPublishing__Mode=Http
EventPublishing__Endpoint=https://<your-sentraai-api>.azurewebsites.net/api/home-events
EventPublishing__ApiKey=<your-api-key>
SmartHomeIntegration__Provider=Vera
Vera__BaseUrl=http://192.168.1.50:3480/
```

## Docker local API + connector

```bash
docker compose -f docker-compose.local.yml up --build
```

## Tests

```bash
dotnet test
```

## Production next steps

1. Replace `InMemorySentraAIStore` with PostgreSQL/EF Core or Azure SQL.
2. Replace `HttpHomeEventPublisher` with Azure Service Bus publisher/consumer for reliable cloud ingestion.
3. Replace `FakeLlmClient` with Azure OpenAI/OpenAI/Ollama implementation.
4. Add user approval UI before any physical action executor.
5. Add durable idempotency for `HomeEvent.Id` and connector retries.
6. Add Application Insights/OpenTelemetry exporter after selecting final package versions.
