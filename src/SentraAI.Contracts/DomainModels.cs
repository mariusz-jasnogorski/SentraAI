namespace SentraAI.Contracts;

public enum HomeEventType { Unknown = 0, MotionDetected = 1, DoorOpened = 2, WindowOpened = 3, TemperatureChanged = 4, LightChanged = 5, DeviceStateChanged = 6 }
public enum AgentFindingSeverity { Info = 0, Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum RecommendationActionType { None = 0, NotifyUser = 1, SuggestAutomation = 2, RequestApproval = 3, ExecuteAfterApproval = 4 }

public sealed record HomeEvent(string Id, string Source, string DeviceId, string DeviceName, string Room, HomeEventType Type, string Value, DateTimeOffset OccurredAt, IReadOnlyDictionary<string, string>? Metadata = null);
public sealed record DeviceState(string DeviceId, string DeviceName, string Room, string Kind, string CurrentValue, DateTimeOffset LastUpdatedAt, IReadOnlyDictionary<string, string>? Metadata = null);
public sealed record SentraAIContext(IReadOnlyList<DeviceState> Devices, IReadOnlyList<HomeEvent> RecentEvents, IReadOnlyDictionary<string, string>? UserPreferences = null, IReadOnlyDictionary<string, string>? Environment = null);
public sealed record AgentEvidence(string Source, string Description, DateTimeOffset ObservedAt);

public sealed record AgentFinding(string Id, string AgentName, string Title, string Description, AgentFindingSeverity Severity, double Confidence, IReadOnlyList<AgentEvidence> Evidence, RecommendationActionType SuggestedActionType, string? SuggestedActionPayload = null, DateTimeOffset CreatedAt = default)
{
    public DateTimeOffset CreatedAt { get; init; } = CreatedAt == default ? DateTimeOffset.UtcNow : CreatedAt;
}

public sealed record Recommendation(string Id, string FindingId, string Title, string Message, RecommendationActionType ActionType, bool RequiresUserApproval, DateTimeOffset CreatedAt, string? Payload = null);
public sealed record UserMessage(string Title, string Body, AgentFindingSeverity Severity, DateTimeOffset CreatedAt);
