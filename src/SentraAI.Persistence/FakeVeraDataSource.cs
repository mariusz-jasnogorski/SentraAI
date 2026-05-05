using SentraAI.Contracts;

namespace SentraAI.Persistence;

/// <summary>
/// Local simulator that produces Vera-like normalized events.
///
/// In real production code this class would be replaced by VeraCollector, which calls:
/// http://VERA_IP:3480/data_request?id=sdata&output_format=json
/// and maps Vera devices/variables into HomeEvent records.
/// </summary>
public sealed class FakeVeraDataSource
{
    public IReadOnlyList<HomeEvent> GenerateDemoEvents()
    {
        var now = DateTimeOffset.UtcNow;

        // This scenario intentionally creates an anomaly:
        // - living room window is open
        // - heating is on
        // - temperature drops rapidly
        return
        [
            new HomeEvent(Guid.NewGuid(), "temp-living", "Living Room Temperature", "LivingRoom", "Temperature", "21.2", now.AddMinutes(-30)),
            new HomeEvent(Guid.NewGuid(), "heat-living", "Living Room Heating", "LivingRoom", "Heating", "On", now.AddMinutes(-25)),
            new HomeEvent(Guid.NewGuid(), "window-living", "Living Room Window", "LivingRoom", "Window", "Open", now.AddMinutes(-20)),
            new HomeEvent(Guid.NewGuid(), "temp-living", "Living Room Temperature", "LivingRoom", "Temperature", "18.1", now.AddMinutes(-5)),

            // Repeated behavior used by AutomationDiscoveryAgent:
            // motion in kitchen followed shortly by light being turned on.
            new HomeEvent(Guid.NewGuid(), "motion-kitchen", "Kitchen Motion", "Kitchen", "Motion", "Detected", now.AddMinutes(-50)),
            new HomeEvent(Guid.NewGuid(), "light-kitchen", "Kitchen Light", "Kitchen", "Light", "On", now.AddMinutes(-49)),
            new HomeEvent(Guid.NewGuid(), "motion-kitchen", "Kitchen Motion", "Kitchen", "Motion", "Detected", now.AddMinutes(-40)),
            new HomeEvent(Guid.NewGuid(), "light-kitchen", "Kitchen Light", "Kitchen", "Light", "On", now.AddMinutes(-39)),
            new HomeEvent(Guid.NewGuid(), "motion-kitchen", "Kitchen Motion", "Kitchen", "Motion", "Detected", now.AddMinutes(-30)),
            new HomeEvent(Guid.NewGuid(), "light-kitchen", "Kitchen Light", "Kitchen", "Light", "On", now.AddMinutes(-29)),
        ];
    }
}
