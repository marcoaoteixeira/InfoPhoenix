using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> OpenCurrentDocumentErrorDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to open current document. File: {FilePath}"
        );

    extension(ILogger<SearchResultsSliderWindowViewModel> self) {
        internal void OpenCurrentDocumentError(string filePath, Exception exception) {
            OpenCurrentDocumentErrorDelegate(self, filePath, exception);
        }
    }
}
