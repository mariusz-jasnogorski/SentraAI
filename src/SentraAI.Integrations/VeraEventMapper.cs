using SentraAI.Contracts;

namespace SentraAI.Integrations;

public static class VeraEventMapper
{
    public static HomeEvent Map(VeraDevice device, DateTimeOffset observedAt)
    {
        var deviceId = $"vera-{device.Id}";
        var name = string.IsNullOrWhiteSpace(device.Name) ? deviceId : device.Name!;
        var room = string.IsNullOrWhiteSpace(device.Room) ? "Unknown" : device.Room!;
        var metadata = new Dictionary<string, string>
        {
            ["category"] = device.Category.ToString(),
            ["subcategory"] = device.Subcategory.ToString()
        };

        if (!string.IsNullOrWhiteSpace(device.Temperature))
            return new HomeEvent(Guid.NewGuid().ToString("N"), "Vera", deviceId, name, room, HomeEventType.TemperatureChanged, device.Temperature!, observedAt, metadata);

        if (string.Equals(device.Tripped, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(device.Tripped, "true", StringComparison.OrdinalIgnoreCase))
            return new HomeEvent(Guid.NewGuid().ToString("N"), "Vera", deviceId, name, room, HomeEventType.MotionDetected, "true", observedAt, metadata);

        if (!string.IsNullOrWhiteSpace(device.Status))
        {
            var value = device.Status is "1" ? "on" : device.Status is "0" ? "off" : device.Status!;
            return new HomeEvent(Guid.NewGuid().ToString("N"), "Vera", deviceId, name, room, HomeEventType.DeviceStateChanged, value, observedAt, metadata);
        }

        return new HomeEvent(Guid.NewGuid().ToString("N"), "Vera", deviceId, name, room, HomeEventType.Unknown, "unknown", observedAt, metadata);
    }
}
