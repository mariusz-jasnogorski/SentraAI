namespace SentraAI.Contracts;

public interface ISmartHomeEventSource { Task<IReadOnlyList<HomeEvent>> ReadEventsAsync(CancellationToken cancellationToken); }

public interface ISentraAIStore
{
    Task AddEventsAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken);
    Task<IReadOnlyList<HomeEvent>> GetRecentEventsAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviceState>> GetCurrentDeviceStatesAsync(CancellationToken cancellationToken);
    Task AddFindingsAsync(IReadOnlyList<AgentFinding> findings, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentFinding>> GetFindingsAsync(CancellationToken cancellationToken);
    Task AddRecommendationsAsync(IReadOnlyList<Recommendation> recommendations, CancellationToken cancellationToken);
    Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(CancellationToken cancellationToken);
}

public interface ISentraAIContextBuilder { Task<SentraAIContext> BuildAsync(CancellationToken cancellationToken); }
public interface IAgent<in TContext, TResult> { string Name { get; } Task<TResult> AnalyzeAsync(TContext context, CancellationToken cancellationToken); }
public interface ISentraAIAgent : IAgent<SentraAIContext, IReadOnlyList<AgentFinding>>;
public interface IPolicyEngine { Task<IReadOnlyList<AgentFinding>> FilterAsync(IReadOnlyList<AgentFinding> findings, CancellationToken cancellationToken); }
public interface IRecommendationService { Task<IReadOnlyList<Recommendation>> CreateRecommendationsAsync(IReadOnlyList<AgentFinding> findings, CancellationToken cancellationToken); }
public interface ISentraAIAgentPipeline { Task<IReadOnlyList<Recommendation>> RunAsync(SentraAIContext context, CancellationToken cancellationToken); }
public interface INotificationChannel { Task SendAsync(UserMessage message, CancellationToken cancellationToken); }
public interface ICommunicationAgent { Task<UserMessage> CreateMessageAsync(Recommendation recommendation, CancellationToken cancellationToken); }
public interface IHomeEventPublisher { Task PublishAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken); }
public interface ISentraAIOrchestrator
{
    Task<IReadOnlyList<Recommendation>> RunOnceAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Recommendation>> ProcessEventsAsync(IReadOnlyList<HomeEvent> events, CancellationToken cancellationToken);
}
public interface ILlmClient { Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken); }
public interface IAgentTool { string Name { get; } Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken); }

public sealed record LlmRequest(string SystemPrompt, string UserPrompt, IReadOnlyList<AgentToolDescriptor> Tools, int MaxSteps = 4);
public sealed record LlmResponse(string Content, string? ToolName = null, string? ToolArguments = null, bool IsToolCall = false);
public sealed record AgentToolDescriptor(string Name, string Description, string ParametersJsonSchema);
