using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;

public sealed record DocumentDirectorySavedNotification : NotificationBase {
    public Guid Id  { get; init; }
}