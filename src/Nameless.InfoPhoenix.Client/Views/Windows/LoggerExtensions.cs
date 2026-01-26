using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, Exception> SetWindowIconFailureDelegate =
        LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to set the main window icon."
        );

    extension(ILogger<MainWindow> self) {
        internal void SetWindowIconFailure(Exception exception) {
            SetWindowIconFailureDelegate(self, exception);
        }
    }
}
