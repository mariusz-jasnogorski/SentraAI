using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Detects opportunities for automation based on repeated user behavior.
///
/// In production, this would be more advanced:
/// - sequence mining
/// - time windows
/// - user feedback
/// - confidence scoring
/// - suppression rules after repeated rejection
///
/// This MVP demonstrates the concept with a simple pattern:
/// motion detected in a room is repeatedly followed by light switched on.
/// </summary>
public sealed class AutomationDiscoveryAgent : ISentraAIAgent
{
    public Task<AgentFinding?> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        var events = context.RecentEvents.OrderBy(x => x.Timestamp).ToList();

        var candidateRooms = events
            .Where(x => x.Type == "Motion" && x.Value == "Detected")
            .Select(x => x.Room)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var room in candidateRooms)
        {
            var motionEvents = events
                .Where(x => x.Room == room && x.Type == "Motion" && x.Value == "Detected")
                .ToList();

            var matches = 0;

            foreach (var motion in motionEvents)
            {
                // Count cases where the light was turned on shortly after motion.
                var followedByLightOn = events.Any(x =>
                    x.Room == room &&
                    x.Type == "Light" &&
                    x.Value == "On" &&
                    x.Timestamp > motion.Timestamp &&
                    x.Timestamp <= motion.Timestamp.AddMinutes(2));

                if (followedByLightOn)
                    matches++;
            }

            if (matches < 3)
                continue;

            var finding = new AgentFinding(
                Type: "AutomationOpportunity",
                Title: $"Possible automatic light rule for {room}",
                Description: $"Motion in {room} was repeatedly followed by turning the light on. The system can suggest an automation.",
                Severity: "Low",
                Confidence: 0.82,
                Evidence:
                [
                    $"Repeated motion → light-on sequence detected in {room}",
                    $"Occurrences in recent history: {matches}"
                ],
                RecommendedActions:
                [
                    $"Ask user whether to turn on the light automatically when motion is detected in {room}"
                ]);

            return Task.FromResult<AgentFinding?>(finding);
        }

        return Task.FromResult<AgentFinding?>(null);
    }
}
