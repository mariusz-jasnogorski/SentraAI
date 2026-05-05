using SentraAI.Contracts;

namespace SentraAI.Notifications;

/// <summary>
/// Converts recommendations into user-facing messages.
///
/// In a production system this class could call an LLM to rewrite technical findings into
/// friendlier language. For critical alerts, templates are usually safer than LLM text.
/// </summary>
public sealed class CommunicationAgent
{
    public UserMessage CreateMessage(Recommendation recommendation)
    {
        var priority = recommendation.Finding.Severity switch
        {
            "Critical" => "Critical",
            "High" => "High",
            "Medium" => "Medium",
            _ => "Low"
        };

        return new UserMessage(
            Guid.NewGuid(),
            recommendation.MessageTitle,
            recommendation.MessageBody,
            priority,
            DateTimeOffset.UtcNow);
    }
}

/// <summary>
/// Notification channel abstraction.
/// Add implementations for dashboard, email, push, SMS, Telegram, etc.
/// </summary>
public interface INotificationChannel
{
    Task SendAsync(UserMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Local demo notification channel.
/// </summary>
public sealed class ConsoleNotificationChannel : INotificationChannel
{
    public Task SendAsync(UserMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine("=== USER MESSAGE ===");
        Console.WriteLine($"Priority: {message.Priority}");
        Console.WriteLine($"Title:    {message.Title}");
        Console.WriteLine($"Body:     {message.Body}");
        Console.WriteLine("====================");
        Console.WriteLine();

        return Task.CompletedTask;
    }
}

/// <summary>
/// Routes messages to one or more channels.
/// In production this is the right place for anti-spam rules and user preferences.
/// </summary>
public sealed class NotificationRouter
{
    private readonly IEnumerable<INotificationChannel> _channels;

    public NotificationRouter(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels;
    }

    public async Task RouteAsync(UserMessage message, CancellationToken cancellationToken)
    {
        foreach (var channel in _channels)
        {
            await channel.SendAsync(message, cancellationToken);
        }
    }
}
