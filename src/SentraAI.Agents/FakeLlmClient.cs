using SentraAI.Contracts;

namespace SentraAI.Agents;

public sealed class FakeLlmClient : ILlmClient
{
    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        const string json = "{\"title\":\"LLM context review\",\"description\":\"No additional LLM-only anomaly was found in the compact context.\",\"severity\":\"Info\",\"confidence\":0.50}";
        return Task.FromResult(new LlmResponse(json));
    }
}
