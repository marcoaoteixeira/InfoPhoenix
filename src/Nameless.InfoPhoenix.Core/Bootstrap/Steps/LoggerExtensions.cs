using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;

namespace Nameless.InfoPhoenix.Bootstrap.Steps;

/// <summary>
///     Extension methods for <see cref="ILogger"/> that target
///     the <see cref="AppDbContext"/> implementations.
/// </summary>
internal static class LoggerExtensions {
    private static readonly Action<ILogger, Exception?> SkipMigrationForNonRelationalDatabaseDelegate
        = LoggerMessage.Define(
            logLevel: LogLevel.Information,
            eventId: default,
            formatString: "DbContext references a non-relational database, skipping migration action."
        );

    extension(ILogger<InitializeDbContextStep> self) {
        internal void SkipMigrationForNonRelationalDatabase() {
            SkipMigrationForNonRelationalDatabaseDelegate(self, null /* exception */);
        }
    }
}
