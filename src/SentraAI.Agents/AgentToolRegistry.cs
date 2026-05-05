using SentraAI.Contracts;

namespace SentraAI.Agents;

/// <summary>
/// Allowlist of tools available to the LLM agent.
///
/// This is a safety boundary. The model cannot call arbitrary code, SQL or HTTP.
/// It can only request tools registered here.
/// </summary>
public sealed class AgentToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools;

    public AgentToolRegistry(IEnumerable<IAgentTool> tools)
    {
        _tools = tools.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IAgentTool> GetAll() => _tools.Values;

    public bool TryGet(string name, out IAgentTool tool) => _tools.TryGetValue(name, out tool!);
}
