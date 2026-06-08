using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Connector.Worker;

public sealed class ConnectorWorker(
    ISmartHomeEventSource source,
    IHomeEventPublisher publisher,
    IOptions<ConnectorOptions> options,
    ILogger<ConnectorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollingIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var events = await source.ReadEventsAsync(stoppingToken);
                logger.LogInformation("Connector read {Count} events", events.Count);
                await publisher.PublishAsync(events, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Connector cycle failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
