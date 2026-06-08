using Microsoft.Extensions.Options;
using SentraAI.Contracts;
using System.Net.Http.Json;

namespace SentraAI.Integrations;

public sealed class VeraSmartHomeEventSource(HttpClient httpClient, IOptions<VeraOptions> options) : ISmartHomeEventSource
{
    public async Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken)
    {
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/data_request?id=sdata&output_format=json";
        var response = await httpClient.GetFromJsonAsync<VeraSDataResponse>(url, cancellationToken) ?? new VeraSDataResponse();
        var now = DateTimeOffset.UtcNow;
        return response.Devices.Select(x => VeraEventMapper.Map(x, now)).ToList();
    }
}
