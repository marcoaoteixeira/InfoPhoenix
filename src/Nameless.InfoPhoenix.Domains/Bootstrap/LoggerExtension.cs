using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Domains.Bootstrap;

internal static class LoggerExtension {
    private static readonly Action<ILogger<InitializeAppDbContextStep>, Exception?> NonRelationalDatabaseSkipMigrationDelegate
        = LoggerMessage.Define(logLevel: LogLevel.Information,
                               eventId: default,
                               formatString: "Non-relational database detected. Skipping migration routine.",
                               options: null);

    internal static void NonRelationalDatabaseSkipMigration(this ILogger<InitializeAppDbContextStep> self)
        => NonRelationalDatabaseSkipMigrationDelegate(self, null /* exception */);
}