using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.List;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, Exception> ListDocumentDirectoriesFailureDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to retrieve all document directories."
        );

    extension(ILogger self) {
        internal void ListDocumentDirectoriesFailure(Exception exception) {
            ListDocumentDirectoriesFailureDelegate(self, exception);
        }
    }
}
