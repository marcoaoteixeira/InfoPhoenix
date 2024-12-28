using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Application.Notification;

public sealed record SnackbarParameters {
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public ControlAppearance Appearance { get; init; }
}