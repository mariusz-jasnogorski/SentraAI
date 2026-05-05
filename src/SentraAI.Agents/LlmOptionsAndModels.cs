using System.Text.Json;
using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Runtime options controlling the LLM agent.
/// These limits are important in production to avoid infinite/expensive reasoning loops.
/// </summary>
public sealed class LlmAgentOptions
{
    public int MaxSteps { get; set; } = 4;
    public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public double MinimumConfidence { get; set; } = 0.70;
}

/// <summary>
/// Parsed decision returned by the LLM.
/// It can be either a tool call request or final structured result.
/// </summary>
public sealed class AgentModelDecision
{
    public bool IsFinal { get; init; }
    public string? ToolName { get; init; }
    public JsonElement? ToolArguments { get; init; }
    public AgentFinding? FinalResult { get; init; }
}
