using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception?> InsertDocumentsFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Warning,
            eventId: default,
            formatString: "An error occurred while trying to insert documents into index. Error: {Error}"
        );

    extension(ILogger self) {
        internal void InsertDocumentsFailure(string error) {
            InsertDocumentsFailureDelegate(self, error, null /* exception */);
        }

        internal void DeleteDocumentsFailure(string error) {
            InsertDocumentsFailureDelegate(self, error, null /* exception */);
        }
    }
}
