using Microsoft.Extensions.Options;
using SentraAI.Contracts;
using SentraAI.Integrations;

namespace SentraAI.Agents.Tests;

public sealed class DeviceStateTrackerTests
{
    [Fact]
    public void TrackChanges_uses_first_snapshot_as_baseline_by_default()
    {
        var tracker = CreateTracker();
        var now = DateTimeOffset.UtcNow;

        var changes = tracker.TrackChanges([
            State("vera-1", "Motion", "false", now)
        ], now);

        Assert.Empty(changes);
    }

    [Fact]
    public void TrackChanges_emits_event_when_state_value_changes()
    {
        var tracker = CreateTracker();
        var now = DateTimeOffset.UtcNow;

        tracker.TrackChanges([
            State("vera-1", "Motion", "false", now)
        ], now);

        var changes = tracker.TrackChanges([
            State("vera-1", "Motion", "true", now.AddSeconds(5))
        ], now.AddSeconds(5));

        var change = Assert.Single(changes);
        Assert.Equal(HomeEventType.MotionDetected, change.Type);
        Assert.Equal("true", change.Value);
        Assert.Equal("false", change.Metadata!["previousValue"]);
        Assert.Equal("true", change.Metadata!["currentValue"]);
    }

    [Fact]
    public void TrackChanges_does_not_emit_event_when_value_is_unchanged()
    {
        var tracker = CreateTracker();
        var now = DateTimeOffset.UtcNow;

        tracker.TrackChanges([
            State("vera-1", "Switch", "off", now)
        ], now);

        var changes = tracker.TrackChanges([
            State("vera-1", "Switch", "off", now.AddSeconds(5))
        ], now.AddSeconds(5));

        Assert.Empty(changes);
    }

    [Fact]
    public void TrackChanges_can_emit_initial_snapshot_when_enabled()
    {
        var tracker = CreateTracker(emitInitialSnapshot: true);
        var now = DateTimeOffset.UtcNow;

        var changes = tracker.TrackChanges([
            State("vera-1", "Temperature", "22.5", now)
        ], now);

        var change = Assert.Single(changes);
        Assert.Equal(HomeEventType.TemperatureChanged, change.Type);
        Assert.Equal("true", change.Metadata!["isInitialSnapshot"]);
    }

    private static InMemoryDeviceStateTracker CreateTracker(bool emitInitialSnapshot = false)
    {
        return new InMemoryDeviceStateTracker(Options.Create(new StateTrackingOptions
        {
            EmitInitialSnapshot = emitInitialSnapshot
        }));
    }

    private static DeviceState State(string id, string kind, string value, DateTimeOffset updatedAt)
    {
        return new DeviceState(
            id,
            $"Device {id}",
            "Hall",
            kind,
            value,
            updatedAt,
            new Dictionary<string, string> { ["source"] = "Vera" });
    }
}
