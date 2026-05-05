using Microsoft.Extensions.Logging;
using SentraAI.Contracts;
using SentraAI.Persistence;

namespace SentraAI.Agents;

/// <summary>
/// Policy engine validates whether a finding is safe and useful enough to become a recommendation.
///
/// This is separate from agents because agents can be wrong. Especially LLM-based agents should
/// never be trusted blindly.
/// </summary>
public interface IPolicyEngine
{
    Task<bool> AllowAsync(AgentFinding finding, CancellationToken cancellationToken);
}

public sealed class DefaultPolicyEngine : IPolicyEngine
{
    public Task<bool> AllowAsync(AgentFinding finding, CancellationToken cancellationToken)
    {
        // Simple production-like safeguards.
        if (finding.Confidence < 0.70)
            return Task.FromResult(false);

        if (finding.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase) && finding.Evidence.Count < 2)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}

/// <summary>
/// Converts an approved finding into a recommendation stored in the system.
/// </summary>
public sealed class RecommendationService
{
    private readonly InMemorySentraAIStore _store;

    public RecommendationService(InMemorySentraAIStore store)
    {
        _store = store;
    }

    public Task<Recommendation> CreateAsync(AgentFinding finding, CancellationToken cancellationToken)
    {
        var recommendation = new Recommendation(
            Guid.NewGuid(),
            finding,
            finding.Title,
            finding.Description,
            DateTimeOffset.UtcNow);

        _store.AddRecommendation(recommendation);
        return Task.FromResult(recommendation);
    }
}

/// <summary>
/// Main orchestration pipeline.
///
/// It runs multiple agents, validates each finding, persists approved results and creates
/// recommendations. This class is intentionally ignorant of notification details.
/// </summary>
public sealed class SentraAIAgentPipeline
{
    private readonly IEnumerable<ISentraAIAgent> _agents;
    private readonly IPolicyEngine _policyEngine;
    private readonly RecommendationService _recommendationService;
    private readonly InMemorySentraAIStore _store;
    private readonly ILogger<SentraAIAgentPipeline> _logger;

    public SentraAIAgentPipeline(
        IEnumerable<ISentraAIAgent> agents,
        IPolicyEngine policyEngine,
        RecommendationService recommendationService,
        InMemorySentraAIStore store,
        ILogger<SentraAIAgentPipeline> logger)
    {
        _agents = agents;
        _policyEngine = policyEngine;
        _recommendationService = recommendationService;
        _store = store;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Recommendation>> ProcessAsync(
        SentraAIContext context,
        CancellationToken cancellationToken)
    {
        var recommendations = new List<Recommendation>();

        foreach (var agent in _agents)
        {
            var finding = await agent.AnalyzeAsync(context, cancellationToken);

            if (finding is null)
                continue;

            _store.AddFinding(finding);

            var allowed = await _policyEngine.AllowAsync(finding, cancellationToken);

            if (!allowed)
            {
                _logger.LogInformation("Finding rejected by policy: {Title}", finding.Title);
                continue;
            }

            var recommendation = await _recommendationService.CreateAsync(finding, cancellationToken);
            recommendations.Add(recommendation);

            _logger.LogInformation("Recommendation created: {Title}", recommendation.MessageTitle);
        }

        return recommendations;
    }
}
