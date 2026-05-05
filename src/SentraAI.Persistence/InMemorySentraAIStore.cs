using SentraAI.Contracts;

namespace SentraAI.Persistence;

/// <summary>
/// Very small in-memory store used for local demo mode.
///
/// Production replacement:
/// - PostgreSQL for HomeEvents, DeviceReadings, Findings, Recommendations
/// - pgvector or Azure AI Search for situation memory / RAG
/// - Redis for short-lived state/cache
/// </summary>
public sealed class InMemorySentraAIStore
{
    private readonly List<HomeEvent> _events = [];
    private readonly List<AgentFinding> _findings = [];
    private readonly List<Recommendation> _recommendations = [];

    private readonly object _lock = new();

    /// <summary>
    /// Appends normalized events. The store is append-only for event history.
    /// </summary>
    public void AddEvents(IEnumerable<HomeEvent> events)
    {
        lock (_lock)
        {
            _events.AddRange(events);
        }
    }

    /// <summary>
    /// Returns the latest events. Agents use this as recent context.
    /// </summary>
    public IReadOnlyList<HomeEvent> GetRecentEvents(int take = 100)
    {
        lock (_lock)
        {
            return _events
                .OrderByDescending(x => x.Timestamp)
                .Take(take)
                .ToList();
        }
    }

    /// <summary>
    /// Builds latest device state from event history.
    /// For each device/type pair, the newest event wins.
    /// </summary>
    public IReadOnlyList<DeviceState> GetCurrentDeviceStates()
    {
        lock (_lock)
        {
            return _events
                .GroupBy(x => new { x.DeviceId, x.Type })
                .Select(g => g.OrderByDescending(x => x.Timestamp).First())
                .Select(x => new DeviceState(
                    x.DeviceId,
                    x.DeviceName,
                    x.Room,
                    x.Type,
                    x.Value,
                    x.Timestamp))
                .ToList();
        }
    }

    public void AddFinding(AgentFinding finding)
    {
        lock (_lock)
        {
            _findings.Add(finding);
        }
    }

    public IReadOnlyList<AgentFinding> GetFindings()
    {
        lock (_lock)
        {
            return _findings.ToList();
        }
    }

    public void AddRecommendation(Recommendation recommendation)
    {
        lock (_lock)
        {
            _recommendations.Add(recommendation);
        }
    }

    public IReadOnlyList<Recommendation> GetRecommendations()
    {
        lock (_lock)
        {
            return _recommendations.ToList();
        }
    }
}

/// <summary>
/// Builds a compact SentraAIContext for agents.
/// In production this would combine current state, recent events, active automations,
/// user preferences, weather, occupancy and perhaps selected RAG memories.
/// </summary>
public sealed class SentraAIContextBuilder
{
    private readonly InMemorySentraAIStore _store;

    public SentraAIContextBuilder(InMemorySentraAIStore store)
    {
        _store = store;
    }

    public Task<SentraAIContext> BuildAsync(CancellationToken cancellationToken)
    {
        var context = new SentraAIContext(
            DateTimeOffset.UtcNow,
            _store.GetCurrentDeviceStates(),
            _store.GetRecentEvents());

        return Task.FromResult(context);
    }
}
