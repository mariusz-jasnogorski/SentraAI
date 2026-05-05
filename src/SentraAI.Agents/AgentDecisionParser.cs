using System.Text.Json;
using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Converts JSON returned by the LLM into a strongly typed decision.
///
/// Production note:
/// Prefer provider-native structured outputs / JSON schema where available. This parser is
/// still valuable as a defensive layer.
/// </summary>
public static class AgentDecisionParser
{
    public static AgentModelDecision Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString();

        if (type == "tool_call")
        {
            return new AgentModelDecision
            {
                IsFinal = false,
                ToolName = root.GetProperty("toolName").GetString(),
                ToolArguments = root.GetProperty("arguments").Clone()
            };
        }

        if (type == "final")
        {
            return new AgentModelDecision
            {
                IsFinal = true,
                FinalResult = new AgentFinding(
                    Type: root.GetProperty("findingType").GetString() ?? "LlmAnomaly",
                    Title: root.GetProperty("title").GetString() ?? "LLM finding",
                    Description: root.GetProperty("description").GetString() ?? string.Empty,
                    Severity: root.GetProperty("severity").GetString() ?? "Low",
                    Confidence: root.GetProperty("confidence").GetDouble(),
                    Evidence: ReadStringArray(root, "evidence"),
                    RecommendedActions: ReadStringArray(root, "recommendedActions"))
            };
        }

        throw new InvalidOperationException($"Unsupported LLM response type: {type}");
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
            return [];

        return element
            .EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
    }
}
