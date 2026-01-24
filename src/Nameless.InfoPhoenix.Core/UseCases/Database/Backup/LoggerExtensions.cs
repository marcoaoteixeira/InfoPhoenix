using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, Exception> ExecuteDatabaseBackupFailureDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while creating the database backup file."
        );

    private static readonly Action<ILogger, Exception> PrepareBackupFileFailureDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while preparing the database backup file for storage."
        );

    extension(ILogger<PerformDatabaseBackupRequestHandler> self) {
        internal void ExecuteDatabaseBackupFailure(Exception exception) {
            ExecuteDatabaseBackupFailureDelegate(self, exception);
        }

        internal void PrepareBackupFileFailure(Exception exception) {
            PrepareBackupFileFailureDelegate(self, exception);
        }
    }
}
