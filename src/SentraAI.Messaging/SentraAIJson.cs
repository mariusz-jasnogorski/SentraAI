using System.Text.Json;
using System.Text.Json.Serialization;

namespace SentraAI.Messaging;

public static class SentraAIJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
