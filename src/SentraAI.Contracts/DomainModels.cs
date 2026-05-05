namespace SentraAI.Contracts;

/// <summary>
/// A normalized Sentra AI event.
///
/// Vera Edge/Plus can expose data in its own format. The rest of the system should not
/// depend on that vendor-specific shape. Therefore, the collector/simulator converts
/// everything into this common event model.
/// </summary>
public sealed record HomeEvent(
    Guid Id,
    string DeviceId,
    string DeviceName,
    string Room,
    string Type,
    string Value,
    DateTimeOffset Timestamp);

/// <summary>
/// Current device state used by agents.
///
/// HomeEvent is historical and append-only. DeviceState represents the latest known state
/// of a device/metric and is easier to reason about when detecting anomalies.
/// </summary>
public sealed record DeviceState(
    string DeviceId,
    string DeviceName,
    string Room,
    string Type,
    string Value,
    DateTimeOffset LastUpdatedAt);

/// <summary>
/// Compact context passed to agents.
///
/// In production this object should be intentionally small. Do not pass the whole database
/// or full event history to the LLM. Instead, pass a curated snapshot and expose additional
/// data through controlled tools.
/// </summary>
public sealed record SentraAIContext(
    DateTimeOffset CreatedAt,
    IReadOnlyList<DeviceState> Devices,
    IReadOnlyList<HomeEvent> RecentEvents);

/// <summary>
/// Common output from all agents.
///
/// The key design idea is that agents only produce findings. They do not execute physical
/// actions directly. Execution must go through policy validation and user approval.
/// </summary>
public sealed record AgentFinding(
    string Type,
    string Title,
    string Description,
    string Severity,
    double Confidence,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RecommendedActions);

/// <summary>
/// User-facing recommendation created from an agent finding.
/// </summary>
public sealed record Recommendation(
    Guid Id,
    AgentFinding Finding,
    string MessageTitle,
    string MessageBody,
    DateTimeOffset CreatedAt);

/// <summary>
/// A notification generated for the user.
/// It can later be sent via console, dashboard, push, email, etc.
/// </summary>
public sealed record UserMessage(
    Guid Id,
    string Title,
    string Body,
    string Priority,
    DateTimeOffset CreatedAt);

/// <summary>
/// Generic abstraction for any smart home platform that can produce normalized SentraAI events.
///
/// This is intentionally vendor-neutral. Vera Plus/Edge, Home Assistant, Fibaro, MQTT brokers,
/// KNX gateways or any future integration should implement this contract and return HomeEvent
/// records. The agent pipeline should never depend directly on a vendor API.
/// </summary>
public interface ISmartHomeEventSource
{
    /// <summary>
    /// Reads the latest events or current device state from a smart home platform and converts it
    /// into SentraAI's normalized event model.
    /// </summary>
    Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken);
}
