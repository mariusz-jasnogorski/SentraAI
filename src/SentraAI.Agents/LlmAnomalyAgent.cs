using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Tool-based LLM anomaly agent.
///
/// This is the central agentic component of the demo. It implements a bounded ReAct-like
/// loop:
/// 1. Build prompt from current context and previous observations.
/// 2. Ask LLM what to do next.
/// 3. If LLM requests a tool, execute only an allowlisted tool.
/// 4. Feed observation back to the LLM.
/// 5. Stop when the LLM returns a structured final finding.
///
/// Safety principles:
/// - bounded number of steps
/// - tool allowlist
/// - timeout per tool call
/// - confidence threshold
/// - structured output only
/// - no physical action execution
/// </summary>
public sealed class LlmAnomalyAgent : ILlmAgent, ISentraAIAgent
{
    private readonly ILlmClient _llmClient;
    private readonly AgentToolRegistry _tools;
    private readonly LlmAgentOptions _options;
    private readonly ILogger<LlmAnomalyAgent> _logger;

    public LlmAnomalyAgent(
        ILlmClient llmClient,
        AgentToolRegistry tools,
        IOptions<LlmAgentOptions> options,
        ILogger<LlmAnomalyAgent> logger)
    {
        _llmClient = llmClient;
        _tools = tools;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentFinding?> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken)
    {
        var observations = new List<string>();

        for (var step = 1; step <= _options.MaxSteps; step++)
        {
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(context, observations);

            _logger.LogInformation("LLM anomaly agent step {Step} started", step);

            var rawResponse = await _llmClient.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
            var decision = AgentDecisionParser.Parse(rawResponse);

            if (decision.IsFinal)
            {
                var result = decision.FinalResult;

                if (result is null)
                    return null;

                if (result.Confidence < _options.MinimumConfidence)
                {
                    _logger.LogInformation(
                        "LLM finding discarded because confidence {Confidence} is below threshold {Threshold}",
                        result.Confidence,
                        _options.MinimumConfidence);

                    return null;
                }

                _logger.LogInformation(
                    "LLM anomaly agent produced final finding: {Title}, confidence {Confidence}",
                    result.Title,
                    result.Confidence);

                return result;
            }

            if (string.IsNullOrWhiteSpace(decision.ToolName) || decision.ToolArguments is null)
            {
                _logger.LogWarning("LLM requested invalid tool call");
                return null;
            }

            if (!_tools.TryGet(decision.ToolName, out var tool))
            {
                _logger.LogWarning("LLM requested unknown tool {ToolName}", decision.ToolName);
                return null;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.ToolTimeout);

            var toolResult = await tool.ExecuteAsync(decision.ToolArguments.Value, timeoutCts.Token);

            observations.Add($"Tool={tool.Name}; Success={toolResult.Success}; Data={toolResult.Data}; Error={toolResult.Error}");

            _logger.LogInformation(
                "LLM agent executed tool {ToolName}. Success={Success}",
                tool.Name,
                toolResult.Success);
        }

        _logger.LogInformation("LLM anomaly agent reached max steps without final result");
        return null;
    }

    private static string BuildSystemPrompt()
    {
        // Plain raw string is enough because this prompt does not need C# interpolation.
        return """
        You are a Sentra AI anomaly detection agent.

        Rules:
        - Detect unusual Sentra AI situations.
        - Use tools before making conclusions when data is missing.
        - Do not invent facts.
        - Never execute physical actions directly.
        - Only recommend actions.
        - Return only valid JSON.

        Return one of two JSON shapes:

        Tool call:
        {
          "type": "tool_call",
          "toolName": "query_recent_events",
          "arguments": { "room": "LivingRoom", "take": 20 }
        }

        Final result:
        {
          "type": "final",
          "findingType": "LlmAnomaly",
          "title": "...",
          "description": "...",
          "severity": "Low|Medium|High|Critical",
          "confidence": 0.0,
          "evidence": ["..."],
          "recommendedActions": ["..."]
        }
        """;
    }

    private string BuildUserPrompt(SentraAIContext context, IReadOnlyList<string> observations)
    {
        var toolsDescription = string.Join(
            Environment.NewLine,
            _tools.GetAll().Select(x => $"- {x.Name}: {x.Description}"));

        var compactContext = new
        {
            context.CreatedAt,
            Devices = context.Devices.Select(x => new
            {
                x.Room,
                x.DeviceName,
                x.Type,
                x.Value,
                x.LastUpdatedAt
            })
        };

        // Interpolated raw string with $$ is used because the prompt contains JSON braces.
        // With $$, single { } characters are treated as normal content, while C# interpolation
        // uses double braces like {{variable}}.
        return $$"""
        Available tools:
        {{toolsDescription}}

        Current compact Sentra AI context:
        {{JsonSerializer.Serialize(compactContext)}}

        Previous observations:
        {{string.Join(Environment.NewLine, observations)}}

        Decide the next step.
        """;
    }
}
