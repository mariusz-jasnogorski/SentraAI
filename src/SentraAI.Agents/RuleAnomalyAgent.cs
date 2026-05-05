using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Fast, deterministic anomaly detector.
///
/// This type of agent should be used before LLM analysis because it is:
/// - cheap
/// - predictable
/// - easy to test
/// - safe for critical conditions
///
/// The LLM agent should complement this, not replace it.
/// </summary>
public sealed class RuleAnomalyAgent : ISentraAIAgent
{
    public Task<AgentFinding?> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        // Find rooms where a window is currently open.
        var openWindows = context.Devices
            .Where(x => x.Type == "Window" && x.Value.Equals("Open", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var window in openWindows)
        {
            // Check if heating is still active in the same room.
            var heatingOn = context.Devices.Any(x =>
                x.Room == window.Room &&
                x.Type == "Heating" &&
                x.Value.Equals("On", StringComparison.OrdinalIgnoreCase));

            if (!heatingOn)
                continue;

            // Deterministic finding. No LLM is needed for this obvious rule.
            var finding = new AgentFinding(
                Type: "Anomaly",
                Title: "Open window while heating is active",
                Description: $"The window in {window.Room} is open while heating is still active.",
                Severity: "Medium",
                Confidence: 0.95,
                Evidence:
                [
                    $"Window state in {window.Room}: Open",
                    $"Heating state in {window.Room}: On"
                ],
                RecommendedActions:
                [
                    "Notify the user",
                    $"Suggest turning off heating in {window.Room}"
                ]);

            return Task.FromResult<AgentFinding?>(finding);
        }

        return Task.FromResult<AgentFinding?>(null);
    }
}
