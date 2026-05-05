using Microsoft.Extensions.Options;

namespace SentraAI.Integrations;

/// <summary>
/// Very small HTTP client for Vera Plus / Vera Edge.
///
/// This client intentionally returns raw JSON. Parsing and mapping are handled by VeraEventMapper.
/// Keeping HTTP and mapping separate makes it easier to test the mapper and to replace Vera with
/// another integration later.
/// </summary>
public sealed class VeraClient
{
    private readonly HttpClient _httpClient;

    public VeraClient(HttpClient httpClient, IOptions<VeraOptions> options)
    {
        _httpClient = httpClient;

        var baseUrl = options.Value.BaseUrl;
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    }

    /// <summary>
    /// Gets Vera's compact state JSON.
    ///
    /// Vera exposes multiple local endpoints through data_request. For this MVP we use sdata,
    /// because it is compact and usually enough to discover devices, rooms and current values.
    /// </summary>
    public Task<string> GetSDataJsonAsync(CancellationToken cancellationToken)
    {
        return _httpClient.GetStringAsync(
            "data_request?id=sdata&output_format=json",
            cancellationToken);
    }
}
