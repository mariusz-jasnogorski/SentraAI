using SentraAI.Contracts;
using System.Collections.Concurrent;

namespace SentraAI.Persistence;

public sealed class InMemorySentraAIStore : ISentraAIStore
{
    private readonly object _lock = new();
    private readonly List<HomeEvent> _events = [];
    private readonly List<AgentFinding> _findings = [];
    private readonly List<Recommendation> _recommendations = [];
    private readonly ConcurrentDictionary<string, DeviceState> _deviceStates = new();

    public Task AddEventsAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _events.AddRange(events);
            foreach (var item in events)
            {
                _deviceStates[item.DeviceId] = new DeviceState(item.DeviceId, item.DeviceName, item.Room, item.Type.ToString(), item.Value, item.OccurredAt, item.Metadata);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<HomeEvent>> GetRecentEventsAsync(int take, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<HomeEvent>>(_events.OrderByDescending(x => x.OccurredAt).Take(take).ToList());
        }
    }

    public Task<IReadOnlyList<DeviceState>> GetCurrentDeviceStatesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<DeviceState>>(_deviceStates.Values.OrderBy(x => x.Room).ThenBy(x => x.DeviceName).ToList());

    public Task AddFindingsAsync(IReadOnlyList<AgentFinding> findings, CancellationToken cancellationToken)
    {
        lock (_lock) _findings.AddRange(findings);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AgentFinding>> GetFindingsAsync(CancellationToken cancellationToken)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<AgentFinding>>(_findings.ToList());
    }

    public Task AddRecommendationsAsync(IReadOnlyList<Recommendation> recommendations, CancellationToken cancellationToken)
    {
        lock (_lock) _recommendations.AddRange(recommendations);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(CancellationToken cancellationToken)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<Recommendation>>(_recommendations.ToList());
    }
}
