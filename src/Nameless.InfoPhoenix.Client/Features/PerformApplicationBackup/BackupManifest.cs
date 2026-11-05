namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;

/// <summary>
/// The version/schema marker written at the root of every backup archive (plan IPX-0009 D3),
/// read back by IPX-0011. Unknown JSON members must be ignored on read so a format version 1
/// reader tolerates additive fields from a later ticket.
/// </summary>
/// <param name="FormatVersion">Describes the archive layout. Always <see cref="Constants.BackupRecovery.FormatVersion"/> for now.</param>
/// <param name="Application">Guards against restoring a zip from another application.</param>
/// <param name="AppVersion">The running application's version, verbatim, at backup time.</param>
/// <param name="DatabaseMigrationId">
/// The newest EF Core migration id of the staged database copy, or <see langword="null"/> when
/// there is no database in the archive, or no migrations history table.
/// </param>
/// <param name="CreatedAtUtc">The instant the backup was taken, for display only.</param>
/// <param name="Contents">Which artifacts were actually present at backup time.</param>
public sealed record BackupManifest(
    int FormatVersion,
    string Application,
    string AppVersion,
    string? DatabaseMigrationId,
    DateTimeOffset CreatedAtUtc,
    BackupManifestContents Contents);

/// <summary>
/// Whether each artifact was present when the backup ran. Lets a restore distinguish "artifact
/// absent at backup time" (plan D6) from "archive corrupted".
/// </summary>
public sealed record BackupManifestContents(bool Database, bool SearchIndex, bool Configuration);
