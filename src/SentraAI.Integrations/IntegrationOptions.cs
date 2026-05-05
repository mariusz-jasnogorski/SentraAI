namespace SentraAI.Integrations;

/// <summary>
/// Selects which smart home integration should be used at runtime.
///
/// Provider values supported by the current solution:
/// - "Fake" - local simulator, no hardware required.
/// - "Vera" - real Vera Plus / Vera Edge integration through the local Vera HTTP API.
///
/// Future values can be added without changing the agent pipeline, for example:
/// - "HomeAssistant"
/// - "Fibaro"
/// - "Mqtt"
/// </summary>
public sealed class SmartHomeIntegrationOptions
{
    public string Provider { get; init; } = "Fake";
}

/// <summary>
/// Configuration for Vera Plus / Vera Edge.
///
/// Example BaseUrl:
/// http://192.168.1.50:3480/
///
/// The integration calls:
/// /data_request?id=sdata&output_format=json
/// </summary>
public sealed class VeraOptions
{
    public string BaseUrl { get; init; } = "http://192.168.1.50:3480/";

    /// <summary>
    /// Optional room name overrides by Vera room id.
    /// Useful when the Vera response contains only room ids and not friendly room names.
    /// </summary>
    public Dictionary<string, string> RoomMappings { get; init; } = new();
}
