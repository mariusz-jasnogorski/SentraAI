using SentraAI.Contracts;
using System.Text.Json;

namespace SentraAI.Agents;

public sealed class LlmAnomalyAgent(ILlmClient llmClient, IEnumerable<IAgentTool> tools) : ISentraAIAgent
{
    public string Name => nameof(LlmAnomalyAgent);

    public async Task<IReadOnlyList<AgentFinding>> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        var toolDescriptors = tools.Select(t => new AgentToolDescriptor(t.Name, "Safe read-only SentraAI tool", "{}")).ToList();
        var compactContext = JsonSerializer.Serialize(new
        {
            devices = context.Devices.Select(d => new { d.DeviceName, d.Room, d.Kind, d.CurrentValue, d.LastUpdatedAt }),
            recentEvents = context.RecentEvents.Take(20).Select(e => new { e.DeviceName, e.Room, e.Type, e.Value, e.OccurredAt })
        });

        var response = await llmClient.CompleteAsync(new LlmRequest(
            "You are a read-only smart-home anomaly analyst. You may recommend but never execute physical actions.",
            compactContext,
            toolDescriptors), cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content)) return [];

        try
        {
            using var document = JsonDocument.Parse(response.Content);
            var root = document.RootElement;
            var confidence = root.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5;
            if (confidence < 0.55) return [];

            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "LLM finding" : "LLM finding";
            var description = root.TryGetProperty("description", out var d) ? d.GetString() ?? title : title;
            var severity = root.TryGetProperty("severity", out var s) && Enum.TryParse<AgentFindingSeverity>(s.GetString(), true, out var parsed)
                ? parsed
                : AgentFindingSeverity.Info;

            return [new AgentFinding(Guid.NewGuid().ToString("N"), Name, title, description, severity, confidence,
                [new AgentEvidence("LLM", "Structured LLM review of compact context", DateTimeOffset.UtcNow)],
                RecommendationActionType.NotifyUser)];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
