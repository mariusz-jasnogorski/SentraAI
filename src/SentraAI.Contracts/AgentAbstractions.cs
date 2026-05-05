using System.Text.Json;

namespace SentraAI.Contracts;

/// <summary>
/// Generic agent contract.
///
/// The same interface can be used by rule-based agents, statistical agents, automation
/// discovery agents and LLM-based agents. This keeps the orchestration pipeline simple.
/// </summary>
public interface IAgent<TContext, TResult>
{
    Task<TResult?> AnalyzeAsync(TContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Specialized alias for Sentra AI agents returning AgentFinding.
/// </summary>
public interface ISentraAIAgent : IAgent<SentraAIContext, AgentFinding>
{
}

/// <summary>
/// LLM agent abstraction. It is useful when you want to route between Fake, Local/Ollama,
/// Azure OpenAI or another provider without changing application logic.
/// </summary>
public interface ILlmAgent
{
    Task<AgentFinding?> AnalyzeAsync(SentraAIContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Controlled tool callable by the LLM agent.
///
/// The LLM never gets direct database access. It asks for a tool call in JSON, and the tool
/// validates arguments and decides what data is safe to return.
/// </summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }

    Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}

/// <summary>
/// Standardized tool result.
/// Returning JSON as a string keeps the tool boundary simple and model-friendly.
/// </summary>
public sealed record ToolResult(bool Success, string Data, string? Error = null);

/// <summary>
/// Abstraction over LLM provider.
/// Implementations may call Fake LLM, Ollama, OpenAI, Azure OpenAI, etc.
/// </summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
