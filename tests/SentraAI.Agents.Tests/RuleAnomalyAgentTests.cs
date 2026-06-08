using SentraAI.Agents;
using SentraAI.Contracts;
using Xunit;

namespace SentraAI.Agents.Tests;

public sealed class RuleAnomalyAgentTests
{
    [Fact]
    public async Task AnalyzeAsync_returns_findings_when_rules_match()
    {
        var now = DateTimeOffset.UtcNow;
        var context = new SentraAIContext(
            [new DeviceState("thermo", "Living thermostat", "Living", "Thermostat", "24.5", now)],
            [new HomeEvent("1", "Fake", "door", "Living door", "Living", HomeEventType.DoorOpened, "open", now)]);

        var result = await new RuleAnomalyAgent().AnalyzeAsync(context, CancellationToken.None);

        Assert.NotEmpty(result);
    }
}
