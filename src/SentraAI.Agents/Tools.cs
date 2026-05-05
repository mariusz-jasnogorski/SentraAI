using System.Text.Json;
using SentraAI.Contracts;
using SentraAI.Persistence;

namespace SentraAI.Agents;

/// <summary>
/// Tool that returns recent normalized events from the in-memory store.
///
/// This demonstrates the production pattern:
/// - validate tool arguments
/// - limit returned data
/// - return compact JSON
/// - never expose raw database access to the LLM
/// </summary>
public sealed class QueryRecentEventsTool : IAgentTool
{
    private readonly InMemorySentraAIStore _store;

    public string Name => "query_recent_events";

    public string Description =>
        "Returns recent Sentra AI events. Arguments: { room?: string, take?: number }.";

    public QueryRecentEventsTool(InMemorySentraAIStore store)
    {
        _store = store;
    }

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var take = 50;

        if (arguments.TryGetProperty("take", out var takeElement) && takeElement.ValueKind == JsonValueKind.Number)
            take = Math.Clamp(takeElement.GetInt32(), 1, 100);

        string? room = null;

        if (arguments.TryGetProperty("room", out var roomElement) && roomElement.ValueKind == JsonValueKind.String)
            room = roomElement.GetString();

        var query = _store.GetRecentEvents(take).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(room))
            query = query.Where(x => x.Room.Equals(room, StringComparison.OrdinalIgnoreCase));

        var result = query
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .Select(x => new
            {
                x.Timestamp,
                x.Room,
                x.DeviceName,
                x.Type,
                x.Value
            })
            .ToList();

        return Task.FromResult(new ToolResult(true, JsonSerializer.Serialize(result)));
    }
}

/// <summary>
/// RAG-style tool stub.
///
/// In production this would search similar historical situations using embeddings.
/// For the local demo, it returns a small static memory.
/// </summary>
public sealed class RetrieveSimilarSituationsTool : IAgentTool
{
    public string Name => "retrieve_similar_situations";

    public string Description =>
        "Retrieves historically similar situations. Arguments: { description: string, limit?: number }.";

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("description", out var descriptionElement) ||
            descriptionElement.ValueKind != JsonValueKind.String)
        {
            return Task.FromResult(new ToolResult(false, "", "Missing description."));
        }

        var data = new[]
        {
            new
            {
                summary = "Living room temperature dropped after balcony window was opened while heating was on.",
                outcome = "User accepted recommendation to turn heating off.",
                similarity = 0.91
            }
        };

        return Task.FromResult(new ToolResult(true, JsonSerializer.Serialize(data)));
    }
}
