using System.Diagnostics.CodeAnalysis;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

public readonly record struct PerformDatabaseBackupMetadata(string BackupFilePath) {
    [MemberNotNullWhen(returnValue: true, nameof(BackupFilePath))]
    public bool IsBackupFileAvailable => !string.IsNullOrWhiteSpace(BackupFilePath);
}