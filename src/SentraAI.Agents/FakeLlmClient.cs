using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Fake LLM client for local development and tests.
///
/// It simulates a tool-using LLM:
/// 1. First call asks to query recent events.
/// 2. Second call returns a final structured finding.
///
/// This lets the full agent pipeline work without Azure OpenAI, OpenAI, Ollama or any API key.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    private int _callCount;

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        _callCount++;

        if (_callCount == 1)
        {
            // Raw string without interpolation. JSON braces are treated as normal content.
            return Task.FromResult("""
            {
              "type": "tool_call",
              "toolName": "query_recent_events",
              "arguments": {
                "room": "LivingRoom",
                "take": 20
              }
            }
            """);
        }

        return Task.FromResult("""
        {
          "type": "final",
          "findingType": "LlmAnomaly",
          "title": "Rapid temperature drop in living room",
          "description": "Recent events suggest that the living room temperature dropped while the window was open and heating was still active.",
          "severity": "Medium",
          "confidence": 0.86,
          "evidence": [
            "Recent living room temperature events show a rapid drop",
            "Living room window was open",
            "Living room heating was on"
          ],
          "recommendedActions": [
            "Notify the user",
            "Suggest turning off heating in the living room"
          ]
        }
        """);
    }
}
