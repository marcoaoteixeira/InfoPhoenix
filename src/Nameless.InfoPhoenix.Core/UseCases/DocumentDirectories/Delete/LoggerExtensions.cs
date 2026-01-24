using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> DeleteDocumentDirectoryFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to delete the document directory '{Label}'."
        );

    extension(ILogger self) {
        internal void DeleteDocumentDirectoryFailure(string label, Exception exception) {
            DeleteDocumentDirectoryFailureDelegate(self, label, exception);
        }
    }
}
