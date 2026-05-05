using SentraAI.Agents;
using SentraAI.Contracts;
using Xunit;

namespace SentraAI.Agents.Tests;

public sealed class PolicyEngineTests
{
    [Fact]
    public async Task AllowAsync_ShouldRejectLowConfidenceFinding()
    {
        var policy = new DefaultPolicyEngine();

        var finding = new AgentFinding(
            Type: "Anomaly",
            Title: "Weak signal",
            Description: "The signal is not reliable enough.",
            Severity: "Low",
            Confidence: 0.42,
            Evidence: [],
            RecommendedActions: []);

        var allowed = await policy.AllowAsync(finding, CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    public async Task AllowAsync_ShouldAllowHighConfidenceFinding()
    {
        var policy = new DefaultPolicyEngine();

        var finding = new AgentFinding(
            Type: "Anomaly",
            Title: "Open window with heating",
            Description: "Heating is active while window is open.",
            Severity: "Medium",
            Confidence: 0.95,
            Evidence: ["Window=Open", "Heating=On"],
            RecommendedActions: ["Notify user"]);

        var allowed = await policy.AllowAsync(finding, CancellationToken.None);

        Assert.True(allowed);
    }
}
