using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.Search;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> SearchDocumentsFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while searching documents with term '{Term}'"
        );

    extension(ILogger self) {
        internal void SearchDocumentsFailure(string term, Exception exception) {
            SearchDocumentsFailureDelegate(self, term, exception);
        }
    }
}
