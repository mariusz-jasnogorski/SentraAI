using System.Text.Json;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Integrations;

/// <summary>
/// Maps Vera's vendor-specific JSON into SentraAI's normalized HomeEvent model.
///
/// Vera devices expose different fields depending on device type, plugin and firmware version.
/// Therefore this mapper is intentionally defensive: it tries several common fields and falls
/// back to a generic value instead of failing hard when a device shape is unknown.
/// </summary>
public sealed class VeraEventMapper
{
    private readonly VeraOptions _options;

    public VeraEventMapper(IOptions<VeraOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<HomeEvent> MapSDataJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var roomNames = ReadRooms(root);
        var events = new List<HomeEvent>();

        if (!root.TryGetProperty("devices", out var devices) || devices.ValueKind != JsonValueKind.Array)
        {
            return events;
        }

        foreach (var device in devices.EnumerateArray())
        {
            var deviceId = ReadString(device, "id") ?? Guid.NewGuid().ToString("N");
            var deviceName = ReadString(device, "name") ?? $"Vera Device {deviceId}";
            var room = ResolveRoom(device, roomNames);
            var category = ReadString(device, "category") ?? ReadString(device, "category_num");
            var type = ResolveDeviceType(device, category, deviceName);
            var value = ResolveDeviceValue(device, type);

            events.Add(new HomeEvent(
                Id: Guid.NewGuid(),
                DeviceId: deviceId,
                DeviceName: deviceName,
                Room: room,
                Type: type,
                Value: value,
                Timestamp: DateTimeOffset.UtcNow));
        }

        return events;
    }

    private Dictionary<string, string> ReadRooms(JsonElement root)
    {
        var rooms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("rooms", out var roomArray) && roomArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var room in roomArray.EnumerateArray())
            {
                var id = ReadString(room, "id");
                var name = ReadString(room, "name");

                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                {
                    rooms[id] = name;
                }
            }
        }

        foreach (var mapping in _options.RoomMappings)
        {
            rooms[mapping.Key] = mapping.Value;
        }

        return rooms;
    }

    private string ResolveRoom(JsonElement device, Dictionary<string, string> roomNames)
    {
        var explicitRoomName = ReadString(device, "room_name") ?? ReadString(device, "roomName");
        if (!string.IsNullOrWhiteSpace(explicitRoomName))
        {
            return explicitRoomName;
        }

        var roomId = ReadString(device, "room");
        if (!string.IsNullOrWhiteSpace(roomId) && roomNames.TryGetValue(roomId, out var mappedRoomName))
        {
            return mappedRoomName;
        }

        return !string.IsNullOrWhiteSpace(roomId) ? $"Room-{roomId}" : "Unknown";
    }

    private static string ResolveDeviceType(JsonElement device, string? category, string deviceName)
    {
        var lowerName = deviceName.ToLowerInvariant();

        // Prefer explicit textual hints when present.
        var deviceType = ReadString(device, "device_type") ?? ReadString(device, "deviceType") ?? string.Empty;
        var lowerDeviceType = deviceType.ToLowerInvariant();

        if (lowerName.Contains("temperature") || lowerDeviceType.Contains("temperature")) return "Temperature";
        if (lowerName.Contains("motion") || lowerDeviceType.Contains("motion")) return "Motion";
        if (lowerName.Contains("window") || lowerName.Contains("door")) return "Window";
        if (lowerName.Contains("heat") || lowerName.Contains("thermostat")) return "Heating";
        if (lowerName.Contains("light") || lowerDeviceType.Contains("dimmable") || lowerDeviceType.Contains("binarylight")) return "Light";
        if (lowerName.Contains("power") || lowerName.Contains("energy") || lowerName.Contains("watt")) return "Power";

        // Vera category ids are not always enough, but they are useful as a fallback.
        return category switch
        {
            "2" => "Light",
            "3" => "Light",
            "4" => "Motion",
            "5" => "Heating",
            "16" => "Humidity",
            "17" => "Temperature",
            "21" => "Power",
            _ => "Generic"
        };
    }

    private static string ResolveDeviceValue(JsonElement device, string type)
    {
        // Most useful known Vera fields first.
        var value = type switch
        {
            "Temperature" => ReadString(device, "temperature") ?? ReadString(device, "state"),
            "Humidity" => ReadString(device, "humidity") ?? ReadString(device, "state"),
            "Power" => ReadString(device, "watts") ?? ReadString(device, "power") ?? ReadString(device, "state"),
            "Motion" => NormalizeBinary(ReadString(device, "tripped") ?? ReadString(device, "state") ?? ReadString(device, "status"), "Detected", "Idle"),
            "Window" => NormalizeBinary(ReadString(device, "tripped") ?? ReadString(device, "state") ?? ReadString(device, "status"), "Open", "Closed"),
            "Light" => NormalizeBinary(ReadString(device, "status") ?? ReadString(device, "state"), "On", "Off"),
            "Heating" => NormalizeBinary(ReadString(device, "status") ?? ReadString(device, "state") ?? ReadString(device, "mode"), "On", "Off"),
            _ => null
        };

        return value
            ?? ReadString(device, "state")
            ?? ReadString(device, "status")
            ?? ReadString(device, "level")
            ?? "Unknown";
    }

    private static string? NormalizeBinary(string? raw, string trueValue, string falseValue)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "on" or "open" or "detected" or "tripped" => trueValue,
            "0" or "false" or "off" or "closed" or "idle" or "untripped" => falseValue,
            _ => raw
        };
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}
