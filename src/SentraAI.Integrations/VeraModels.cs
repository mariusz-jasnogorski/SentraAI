using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace SentraAI.Integrations;

public sealed class VeraSDataResponse
{
    [JsonPropertyName("devices")]
    public List<VeraDevice> Devices { get; set; } = new List<VeraDevice>();
}

public sealed class VeraDevice
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("room")]
    [JsonConverter(typeof(NumberToStringConverter))]
    public string? Room { get; set; }
    [JsonPropertyName("category")] public int Category { get; set; }
    [JsonPropertyName("subcategory")] public int Subcategory { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("tripped")] public string? Tripped { get; set; }
    [JsonPropertyName("temperature")] public string? Temperature { get; set; }
}

public sealed class NumberToStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                // Prefer exact integer when possible, otherwise fall back to double representation.
                if (reader.TryGetInt64(out long l))
                    return l.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (reader.TryGetDouble(out double d))
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                // As a last resort return the raw text representation.
                return System.Text.Encoding.UTF8.GetString(reader.ValueSpan);
            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean().ToString();
            case JsonTokenType.Null:
                return null;
            default:
                throw new JsonException($"Cannot convert token of type {reader.TokenType} to string.");
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // If the string is a JSON number (e.g. "123" or "12.3") write it as a number token.
        // Otherwise write as string.
        if (IsJsonNumber(value))
        {
            if (value.IndexOf('.') >= 0 || value.IndexOf('e', StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    writer.WriteNumberValue(d);
                    return;
                }
            }
            else
            {
                if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var l))
                {
                    writer.WriteNumberValue(l);
                    return;
                }
            }
        }

        writer.WriteStringValue(value);
    }

    private static bool IsJsonNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        // basic check: allowed characters in a JSON number
        // optional leading -, digits, optional fraction, optional exponent
        int i = 0;
        if (s[0] == '-') i = 1;
        bool hasDigits = false;
        for (; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsDigit(c)) { hasDigits = true; continue; }
            if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') continue;
            return false;
        }
        return hasDigits;
    }
}