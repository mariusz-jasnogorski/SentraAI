using SentraAI.Contracts;

namespace SentraAI.Integrations;

/// <summary>
/// Vendor-neutral fake event source used for local development and demos.
///
/// This class is deliberately in the Integrations project because, architecturally,
/// it behaves like one of many possible smart home platforms. It lets the rest of the
/// solution run without Vera hardware, network access, Azure credentials or a database.
/// </summary>
public sealed class FakeSmartHomeEventSource : ISmartHomeEventSource
{
    public Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<HomeEvent> events =
        [
            // Anomaly scenario: heating is active while a window is open and temperature drops.
            new HomeEvent(Guid.NewGuid(), "temp-living", "Living Room Temperature", "LivingRoom", "Temperature", "21.2", now.AddMinutes(-30)),
            new HomeEvent(Guid.NewGuid(), "heat-living", "Living Room Heating", "LivingRoom", "Heating", "On", now.AddMinutes(-25)),
            new HomeEvent(Guid.NewGuid(), "window-living", "Living Room Window", "LivingRoom", "Window", "Open", now.AddMinutes(-20)),
            new HomeEvent(Guid.NewGuid(), "temp-living", "Living Room Temperature", "LivingRoom", "Temperature", "18.1", now.AddMinutes(-5)),

            // Repeated behavior used by AutomationDiscoveryAgent.
            new HomeEvent(Guid.NewGuid(), "motion-kitchen", "Kitchen Motion", "Kitchen", "Motion", "Detected", now.AddMinutes(-50)),
            new HomeEvent(Guid.NewGuid(), "light-kitchen", "Kitchen Light", "Kitchen", "Light", "On", now.AddMinutes(-49)),
            new HomeEvent(Guid.NewGuid(), "motion-kitchen", "Kitchen Motion", "Kitchen", "Motion", "Detected", now.AddMinutes(-40)),
            new HomeEvent(Guid.NewGuid(), "light-kitchen", "Kitchen Light", "Kitchen", "Light", "On", now.AddMinutes(-39)),
            new HomeEvent(Guid.NewGuid(), "motion-kitchen", "Kitchen Motion", "Kitchen", "Motion", "Detected", now.AddMinutes(-30)),
            new HomeEvent(Guid.NewGuid(), "light-kitchen", "Kitchen Light", "Kitchen", "Light", "On", now.AddMinutes(-29)),
        ];

        return Task.FromResult(events);
    }
}
