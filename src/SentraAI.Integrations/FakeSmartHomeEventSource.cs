using SentraAI.Contracts;

namespace SentraAI.Integrations;

public sealed class FakeSmartHomeEventSource : ISmartHomeEventSource
{
    public Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<HomeEvent> events =
        [
            new(Guid.NewGuid().ToString("N"), "Fake", "door-front", "Front door", "Hall", HomeEventType.DoorOpened, "open", now.AddMinutes(-4)),
            new(Guid.NewGuid().ToString("N"), "Fake", "thermostat-living", "Living thermostat", "Hall", HomeEventType.TemperatureChanged, "24.8", now.AddMinutes(-3)),
            new(Guid.NewGuid().ToString("N"), "Fake", "motion-garage", "Garage motion", "Garage", HomeEventType.MotionDetected, "true", now.AddMinutes(-1))
        ];
        return Task.FromResult(events);
    }
}
