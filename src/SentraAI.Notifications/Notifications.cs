using SentraAI.Contracts;

namespace SentraAI.Notifications;

public sealed class CommunicationAgent : ICommunicationAgent
{
    public Task<UserMessage> CreateMessageAsync(Recommendation recommendation, CancellationToken cancellationToken)
    {
        var prefix = recommendation.RequiresUserApproval ? "Approval required" : "SentraAI recommendation";
        var body = recommendation.RequiresUserApproval
            ? $"{recommendation.Message}\n\nNo physical smart-home action was executed. Review and approve manually."
            : recommendation.Message;

        return Task.FromResult(new UserMessage($"{prefix}: {recommendation.Title}", body, AgentFindingSeverity.Medium, DateTimeOffset.UtcNow));
    }
}

public sealed class ConsoleNotificationChannel : INotificationChannel
{
    public Task SendAsync(UserMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{message.CreatedAt:O}] {message.Title}");
        Console.WriteLine(message.Body);
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
