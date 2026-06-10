using Microsoft.Extensions.Options;
using SentraAI.Contracts;
using System.Net.Http.Json;

namespace SentraAI.Integrations;

public sealed class VeraSmartHomeStateSource(HttpClient httpClient, IOptions<VeraOptions> options) : ISmartHomeStateSource
{
    public async Task<IReadOnlyList<DeviceState>> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/data_request?id=sdata&output_format=json";
        var response = await httpClient.GetFromJsonAsync<VeraSDataResponse>(url, cancellationToken) ?? new VeraSDataResponse();
        var now = DateTimeOffset.UtcNow;

        return response.Devices
            .Select(device => VeraEventMapper.MapToDeviceState(device, now))
            .Where(state => !string.Equals(state.Kind, "Unknown", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
