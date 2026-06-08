using System.Text.Json.Serialization;

namespace SentraAI.Integrations;

public sealed class VeraSDataResponse
{
    [JsonPropertyName("devices")]
    public List<VeraDevice> Devices { get; set; } = [];
}

public sealed class VeraDevice
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("room")] public string? Room { get; set; }
    [JsonPropertyName("category")] public int Category { get; set; }
    [JsonPropertyName("subcategory")] public int Subcategory { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("tripped")] public string? Tripped { get; set; }
    [JsonPropertyName("temperature")] public string? Temperature { get; set; }
}
