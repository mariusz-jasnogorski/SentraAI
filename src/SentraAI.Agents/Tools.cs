using SentraAI.Contracts;
using System.Text.Json;

namespace SentraAI.Agents;

public sealed class QueryRecentEventsTool(ISentraAIStore store) : IAgentTool
{
    public string Name => "query_recent_events";

    public async Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        var events = await store.GetRecentEventsAsync(20, cancellationToken);
        return JsonSerializer.Serialize(events);
    }
}

public sealed class QueryDeviceStatesTool(ISentraAIStore store) : IAgentTool
{
    public string Name => "query_device_states";

    public async Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        var devices = await store.GetCurrentDeviceStatesAsync(cancellationToken);
        return JsonSerializer.Serialize(devices);
    }
}
