using System.Diagnostics.CodeAnalysis;

namespace Nameless.InfoPhoenix.UseCases.Application.Backup;

public readonly record struct PerformApplicationBackupMetadata(string BackupFilePath) {
    [MemberNotNullWhen(returnValue: true, nameof(BackupFilePath))]
    public bool IsBackupFileAvailable => !string.IsNullOrWhiteSpace(BackupFilePath);
}