using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> GetDocumentContentFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Warning,
            eventId: default,
            formatString: "An error occurred while trying to retrieve content for document '{FilePath}'."
        );

    private static readonly Action<ILogger, string, Exception> GetDocumentRawFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Warning,
            eventId: default,
            formatString: "An error occurred while trying to retrieve document '{FilePath}' raw bytes."
        );

    extension(ILogger self) {
        internal void GetDocumentContentFailure(string filePath, Exception exception) {
            GetDocumentContentFailureDelegate(self, filePath, exception);
        }

        internal void GetDocumentRawFailure(string filePath, Exception exception) {
            GetDocumentRawFailureDelegate(self, filePath, exception);
        }
    }
}
