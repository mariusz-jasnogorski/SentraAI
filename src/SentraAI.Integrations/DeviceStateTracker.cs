using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Integrations;

public sealed class InMemoryDeviceStateTracker(IOptions<StateTrackingOptions> options) : IDeviceStateTracker
{
    private readonly Dictionary<string, DeviceState> _previousStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public IReadOnlyList<HomeEvent> TrackChanges(IReadOnlyList<DeviceState> currentSnapshot, DateTimeOffset observedAt)
    {
        if (currentSnapshot.Count == 0)
        {
            return [];
        }

        var changes = new List<HomeEvent>();

        lock (_sync)
        {
            foreach (var current in currentSnapshot)
            {
                if (!options.Value.IncludeUnknownValues && IsUnknown(current.CurrentValue))
                {
                    continue;
                }

                var key = BuildKey(current);

                if (!_previousStates.TryGetValue(key, out var previous))
                {
                    _previousStates[key] = current;

                    if (options.Value.EmitInitialSnapshot)
                    {
                        changes.Add(CreateEvent(current, previous: null, observedAt, isInitialSnapshot: true));
                    }

                    continue;
                }

                if (string.Equals(previous.CurrentValue, current.CurrentValue, StringComparison.Ordinal))
                {
                    _previousStates[key] = current;
                    continue;
                }

                changes.Add(CreateEvent(current, previous, observedAt, isInitialSnapshot: false));
                _previousStates[key] = current;
            }
        }
        return changes;
    }

    private static string BuildKey(DeviceState state)
    {
        var source = state.Metadata is not null && state.Metadata.TryGetValue("source", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : "unknown";

        return $"{source}:{state.DeviceId}:{state.Kind}";
    }

    private static bool IsUnknown(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static HomeEvent CreateEvent(DeviceState current, DeviceState? previous, DateTimeOffset observedAt, bool isInitialSnapshot)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["kind"] = current.Kind,
            ["currentValue"] = current.CurrentValue,
            ["isInitialSnapshot"] = isInitialSnapshot.ToString().ToLowerInvariant()
        };

        if (current.Metadata is not null)
        {
            foreach (var item in current.Metadata)
            {
                metadata[item.Key] = item.Value;
            }
        }

        if (previous is not null)
        {
            metadata["previousValue"] = previous.CurrentValue;
            metadata["previousLastUpdatedAt"] = previous.LastUpdatedAt.ToString("O");
        }

        var source = metadata.TryGetValue("source", out var sourceValue) && !string.IsNullOrWhiteSpace(sourceValue)
            ? sourceValue
            : "Unknown";

        return new HomeEvent(
            Id: Guid.NewGuid().ToString("N"),
            Source: source,
            DeviceId: current.DeviceId,
            DeviceName: current.DeviceName,
            Room: current.Room,
            Type: ResolveEventType(current.Kind, current.CurrentValue),
            Value: current.CurrentValue,
            OccurredAt: observedAt,
            Metadata: metadata);
    }

    private static HomeEventType ResolveEventType(string kind, string value)
    {
        if (kind.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
        {
            return HomeEventType.TemperatureChanged;
        }

        if (kind.Equals("Motion", StringComparison.OrdinalIgnoreCase))
        {
            return IsActive(value) ? HomeEventType.MotionDetected : HomeEventType.DeviceStateChanged;
        }

        if (kind.Equals("Door", StringComparison.OrdinalIgnoreCase))
        {
            return IsActive(value) ? HomeEventType.DoorOpened : HomeEventType.DeviceStateChanged;
        }

        if (kind.Equals("Window", StringComparison.OrdinalIgnoreCase))
        {
            return IsActive(value) ? HomeEventType.WindowOpened : HomeEventType.DeviceStateChanged;
        }

        if (kind.Equals("Light", StringComparison.OrdinalIgnoreCase) || kind.Equals("Switch", StringComparison.OrdinalIgnoreCase))
        {
            return HomeEventType.LightChanged;
        }

        return HomeEventType.DeviceStateChanged;
    }

    private static bool IsActive(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("open", StringComparison.OrdinalIgnoreCase)
            || value.Equals("on", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("motion", StringComparison.OrdinalIgnoreCase)
            || value.Equals("tripped", StringComparison.OrdinalIgnoreCase);
    }
}
