using SentraAI.Contracts;

namespace SentraAI.Agents;

public sealed class RuleAnomalyAgent : ISentraAIAgent
{
    public string Name => nameof(RuleAnomalyAgent);

    public Task<IReadOnlyList<AgentFinding>> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        var findings = new List<AgentFinding>();
        var now = DateTimeOffset.UtcNow;

        foreach (var evt in context.RecentEvents.Where(x => x.Type is HomeEventType.DoorOpened or HomeEventType.WindowOpened))
        {
            var matchingHeat = context.Devices.FirstOrDefault(d =>
                d.Room.Equals(evt.Room, StringComparison.OrdinalIgnoreCase) &&
                d.DeviceName.Contains("thermostat", StringComparison.OrdinalIgnoreCase));

            if (matchingHeat is not null && double.TryParse(matchingHeat.CurrentValue, out var temp) && temp > 22)
            {
                findings.Add(new AgentFinding(
                    Guid.NewGuid().ToString("N"), Name, "Open contact while heating is high",
                    $"{evt.DeviceName} is open in {evt.Room} while thermostat reports {temp:0.0}°C.",
                    AgentFindingSeverity.Medium, 0.78,
                    [new AgentEvidence(evt.Source, $"{evt.DeviceName}: {evt.Value}", evt.OccurredAt),
                     new AgentEvidence("DeviceState", $"{matchingHeat.DeviceName}: {matchingHeat.CurrentValue}", matchingHeat.LastUpdatedAt)],
                    RecommendationActionType.NotifyUser));
            }
        }

        var nightGarageMotion = context.RecentEvents.FirstOrDefault(e =>
            e.Type == HomeEventType.MotionDetected &&
            e.Room.Contains("garage", StringComparison.OrdinalIgnoreCase) &&
            (now.Hour >= 22 || now.Hour <= 5));

        if (nightGarageMotion is not null)
        {
            findings.Add(new AgentFinding(
                Guid.NewGuid().ToString("N"), Name, "Unexpected night motion",
                $"Motion was detected in {nightGarageMotion.Room} during night hours.",
                AgentFindingSeverity.High, 0.72,
                [new AgentEvidence(nightGarageMotion.Source, $"{nightGarageMotion.DeviceName}: {nightGarageMotion.Value}", nightGarageMotion.OccurredAt)],
                RecommendationActionType.RequestApproval));
        }

        return Task.FromResult<IReadOnlyList<AgentFinding>>(findings);
    }
}
