using Microsoft.Extensions.Logging;
using SentraAI.Contracts;

namespace SentraAI.Integrations;

/// <summary>
/// Real Vera Plus / Vera Edge event source.
///
/// This class implements the generic ISmartHomeEventSource contract, so the rest of SentraAI
/// does not need to know that the data came from Vera. This is the key extensibility point for
/// adding other smart home systems later.
/// </summary>
public sealed class VeraEventSource : ISmartHomeEventSource
{
    private readonly VeraClient _client;
    private readonly VeraEventMapper _mapper;
    private readonly ILogger<VeraEventSource> _logger;

    public VeraEventSource(
        VeraClient client,
        VeraEventMapper mapper,
        ILogger<VeraEventSource> logger)
    {
        _client = client;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reading Vera sdata JSON...");

        var json = await _client.GetSDataJsonAsync(cancellationToken);
        var events = _mapper.MapSDataJson(json);

        _logger.LogInformation("Mapped {EventCount} Vera devices/events into normalized HomeEvent records.", events.Count);

        return events;
    }
}
