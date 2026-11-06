using Microsoft.Data.Sqlite;

namespace Nameless.InfoPhoenix.Client.Data;

/// <summary>
/// Reads the newest EF Core migration id from an already-open SQLite connection (plan IPX-0011
/// §3.1), moved here from <c>PerformApplicationBackupRequestHandler</c> (where it used to be a
/// private static method) so both the backup and restore handlers share one implementation.
/// </summary>
internal static class MigrationHistoryReader {
    /// <summary>
    /// Returns <see langword="null"/> when the migrations history table does not exist.
    /// </summary>
    internal static string? ReadLatestMigrationId(SqliteConnection connection) {
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";

        if (checkCommand.ExecuteScalar() is null) {
            return null;
        }

        using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;";

        return selectCommand.ExecuteScalar() as string;
    }
}
