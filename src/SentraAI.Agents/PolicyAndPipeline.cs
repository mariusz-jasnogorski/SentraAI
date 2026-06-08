using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Agents;

public sealed class DefaultPolicyEngine(IOptions<PolicyOptions> options) : IPolicyEngine
{
    public Task<IReadOnlyList<AgentFinding>> FilterAsync(IReadOnlyList<AgentFinding> findings, CancellationToken cancellationToken)
    {
        var policy = options.Value;
        var accepted = findings.Where(f =>
        {
            if (f.Confidence < policy.MinimumConfidence) return false;
            if (f.Severity == AgentFindingSeverity.Critical)
                return f.Confidence >= policy.CriticalMinimumConfidence && f.Evidence.Count >= policy.CriticalMinimumEvidenceCount;
            return true;
        }).ToList();

        return Task.FromResult<IReadOnlyList<AgentFinding>>(accepted);
    }
}

public sealed class RecommendationService(ISentraAIStore store) : IRecommendationService
{
    public async Task<IReadOnlyList<Recommendation>> CreateRecommendationsAsync(IReadOnlyList<AgentFinding> findings, CancellationToken cancellationToken)
    {
        var recommendations = findings.Select(f => new Recommendation(
            Guid.NewGuid().ToString("N"), f.Id, f.Title, f.Description, f.SuggestedActionType, RequiresApproval(f), DateTimeOffset.UtcNow, f.SuggestedActionPayload)).ToList();

        await store.AddRecommendationsAsync(recommendations, cancellationToken);
        return recommendations;
    }

    private static bool RequiresApproval(AgentFinding finding)
        => finding.Severity >= AgentFindingSeverity.High || finding.SuggestedActionType is RecommendationActionType.RequestApproval or RecommendationActionType.ExecuteAfterApproval;
}

public sealed class SentraAIAgentPipeline(IEnumerable<ISentraAIAgent> agents, IPolicyEngine policyEngine, IRecommendationService recommendationService, ISentraAIStore store, ILogger<SentraAIAgentPipeline> logger) : ISentraAIAgentPipeline
{
    public async Task<IReadOnlyList<Recommendation>> RunAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        var findings = new List<AgentFinding>();
        foreach (var agent in agents)
        {
            var result = await agent.AnalyzeAsync(context, cancellationToken);
            logger.LogInformation("Agent {AgentName} returned {Count} findings", agent.Name, result.Count);
            findings.AddRange(result);
        }

        var accepted = await policyEngine.FilterAsync(findings, cancellationToken);
        await store.AddFindingsAsync(accepted, cancellationToken);
        return await recommendationService.CreateRecommendationsAsync(accepted, cancellationToken);
    }
}
