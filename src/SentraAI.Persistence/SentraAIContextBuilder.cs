using SentraAI.Contracts;

namespace SentraAI.Persistence;

public sealed class SentraAIContextBuilder(ISentraAIStore store) : ISentraAIContextBuilder
{
    public async Task<SentraAIContext> BuildAsync(CancellationToken cancellationToken)
    {
        var devices = await store.GetCurrentDeviceStatesAsync(cancellationToken);
        var events = await store.GetRecentEventsAsync(100, cancellationToken);
        return new SentraAIContext(devices, events);
    }
}
