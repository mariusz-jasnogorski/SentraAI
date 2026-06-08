using Microsoft.Extensions.Options;
using SentraAI.Agents;
using SentraAI.Contracts;
using Xunit;

namespace SentraAI.Agents.Tests;

public sealed class PolicyEngineTests
{
    [Fact]
    public async Task FilterAsync_rejects_low_confidence_finding()
    {
        var engine = new DefaultPolicyEngine(Options.Create(new PolicyOptions { MinimumConfidence = 0.55 }));
        var finding = new AgentFinding(Guid.NewGuid().ToString("N"), "test", "low", "low", AgentFindingSeverity.Low, 0.2, [], RecommendationActionType.NotifyUser);

        var result = await engine.FilterAsync([finding], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterAsync_accepts_high_confidence_finding()
    {
        var engine = new DefaultPolicyEngine(Options.Create(new PolicyOptions { MinimumConfidence = 0.55 }));
        var finding = new AgentFinding(Guid.NewGuid().ToString("N"), "test", "ok", "ok", AgentFindingSeverity.Medium, 0.8, [], RecommendationActionType.NotifyUser);

        var result = await engine.FilterAsync([finding], CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task FilterAsync_rejects_critical_without_enough_evidence()
    {
        var engine = new DefaultPolicyEngine(Options.Create(new PolicyOptions { MinimumConfidence = 0.55, CriticalMinimumConfidence = 0.8, CriticalMinimumEvidenceCount = 2 }));
        var finding = new AgentFinding(Guid.NewGuid().ToString("N"), "test", "critical", "critical", AgentFindingSeverity.Critical, 0.9,
            [new AgentEvidence("test", "one", DateTimeOffset.UtcNow)], RecommendationActionType.RequestApproval);

        var result = await engine.FilterAsync([finding], CancellationToken.None);

        Assert.Empty(result);
    }
}
