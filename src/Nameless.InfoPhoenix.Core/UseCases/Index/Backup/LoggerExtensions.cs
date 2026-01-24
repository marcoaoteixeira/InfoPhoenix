using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.UseCases.Database.Backup;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, Exception> ExecuteDocumentIndexBackupFailureDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while executing the document index backup."
        );

    private static readonly Action<ILogger, Exception> PrepareBackupFileFailureDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while preparing the database backup file for storage."
        );

    extension(ILogger<PerformDocumentIndexBackupRequestHandler> self) {
        internal void ExecuteDocumentIndexBackupFailure(Exception exception) {
            ExecuteDocumentIndexBackupFailureDelegate(self, exception);
        }

        internal void PrepareBackupFileFailure(Exception exception) {
            PrepareBackupFileFailureDelegate(self, exception);
        }
    }
}
