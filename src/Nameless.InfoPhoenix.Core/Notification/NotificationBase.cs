namespace Nameless.InfoPhoenix.Notification;

public abstract record NotificationBase {
    public string? Title { get; init; }
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
}