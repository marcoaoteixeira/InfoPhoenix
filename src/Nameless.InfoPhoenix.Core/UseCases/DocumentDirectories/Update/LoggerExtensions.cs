using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> UpdateDocumentDirectoryFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to update the document directory '{Label}'."
        );

    extension(ILogger self) {
        internal void UpdateDocumentDirectoryFailure(string label, Exception exception) {
            UpdateDocumentDirectoryFailureDelegate(self, label, exception);
        }
    }
}
