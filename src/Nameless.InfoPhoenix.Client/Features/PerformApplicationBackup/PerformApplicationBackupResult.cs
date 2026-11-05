namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;

/// <summary>
/// Outcome of a <see cref="PerformApplicationBackupRequest"/> (plan IPX-0009 §3.1). The
/// handler never throws for an anticipated failure; <see cref="BackupFilePath"/> is
/// <see langword="null"/> on any failure or cancellation.
/// </summary>
/// <param name="BackupFilePath">The full path of the created zip, or <see langword="null"/>.</param>
public sealed record PerformApplicationBackupResult(string? BackupFilePath) {
    public bool Succeeded => BackupFilePath is not null;

    public static PerformApplicationBackupResult Failed => new(BackupFilePath: null);
}
