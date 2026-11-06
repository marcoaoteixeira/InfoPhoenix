using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;

namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;

/// <summary>
/// The rejection a <see cref="BackupCompatibility.Evaluate"/> check returns. <see cref="ReasonKey"/>
/// is the suffix appended to the handler's translation prefix (<c>Rejected_{ReasonKey}</c>).
/// </summary>
internal sealed record BackupRejection(string ReasonKey, object[] Args) {
    internal BackupRejection(string reasonKey) : this(reasonKey, []) { }
}

/// <summary>
/// The decided OD1/D1 compatibility rule ("schema only"): a backup is restorable when the
/// archive's format version and application name match, and - only when the backup actually
/// contains a database - its migration id equals the running app's expected one. A different
/// app version alone is never checked here (plan D1.4): that is a Warning the handler computes
/// separately, because it never rejects anything.
/// </summary>
internal static class BackupCompatibility {
    /// <summary>
    /// Returns <see langword="null"/> when compatible, otherwise the rejection. Checks run in
    /// order - format, application, schema - and the first failure wins.
    /// </summary>
    internal static BackupRejection? Evaluate(BackupManifest manifest, string? expectedMigrationId) {
        if (manifest.FormatVersion != Constants.BackupRecovery.FormatVersion) {
            return new BackupRejection("UnsupportedFormat", [manifest.FormatVersion]);
        }

        if (!string.Equals(manifest.Application, Constants.Application.Name, StringComparison.Ordinal)) {
            return new BackupRejection("WrongApplication", [manifest.Application]);
        }

        if (manifest.Contents.Database &&
            !string.Equals(manifest.DatabaseMigrationId, expectedMigrationId, StringComparison.Ordinal)) {
            return new BackupRejection(
                "SchemaMismatch",
                [manifest.DatabaseMigrationId ?? "-", expectedMigrationId ?? "-"]);
        }

        return null;
    }
}
