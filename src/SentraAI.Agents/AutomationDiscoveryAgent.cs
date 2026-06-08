using SentraAI.Contracts;

namespace SentraAI.Agents;

public sealed class AutomationDiscoveryAgent : ISentraAIAgent
{
    public string Name => nameof(AutomationDiscoveryAgent);

    public Task<IReadOnlyList<AgentFinding>> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        var findings = context.RecentEvents
            .Where(e => e.Type == HomeEventType.MotionDetected && e.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.Room)
            .Where(g => g.Count() >= 2)
            .Select(group => new AgentFinding(
                Guid.NewGuid().ToString("N"), Name, "Potential automation pattern",
                $"Repeated motion in {group.Key} suggests a possible lighting or notification automation.",
                AgentFindingSeverity.Low, 0.61,
                group.Take(3).Select(e => new AgentEvidence(e.Source, $"{e.DeviceName}: {e.Value}", e.OccurredAt)).ToList(),
                RecommendationActionType.SuggestAutomation,
                $"Consider an automation for room '{group.Key}'."))
            .ToList();

        return Task.FromResult<IReadOnlyList<AgentFinding>>(findings);
    }
}
