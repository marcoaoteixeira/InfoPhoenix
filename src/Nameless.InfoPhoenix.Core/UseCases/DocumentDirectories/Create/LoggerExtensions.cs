using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> CreateDocumentDirectoryFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to create the document directory '{Label}'."
        );

    extension(ILogger self) {
        internal void CreateDocumentDirectoryFailure(string label, Exception exception) {
            CreateDocumentDirectoryFailureDelegate(self, label, exception);
        }
    }
}
