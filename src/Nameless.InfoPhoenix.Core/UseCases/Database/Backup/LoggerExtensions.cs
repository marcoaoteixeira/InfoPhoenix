using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, Exception> ExecuteDatabaseBackupFailureDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while executing database backup."
        );

    extension(ILogger<PerformDatabaseBackupRequestHandler> self) {
        internal void ExecuteDatabaseBackupFailure(Exception exception) {
            ExecuteDatabaseBackupFailureDelegate(self, exception);
        }
    }
}
