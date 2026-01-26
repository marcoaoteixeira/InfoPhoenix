using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Get;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, Guid, Exception> GetDocumentDirectoryFailureDelegate
        = LoggerMessage.Define<Guid>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to retrieve the document directory '{DocumentDirectoryID}'."
        );

    extension(ILogger self) {
        internal void GetDocumentDirectoryFailure(Guid documentDirectoryID, Exception exception) {
            GetDocumentDirectoryFailureDelegate(self, documentDirectoryID, exception);
        }
    }
}
