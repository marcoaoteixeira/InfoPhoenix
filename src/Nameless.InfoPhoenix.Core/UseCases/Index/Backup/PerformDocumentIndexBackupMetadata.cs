using System.Diagnostics.CodeAnalysis;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public readonly record struct PerformDocumentIndexBackupMetadata(string BackupFilePath) {
    [MemberNotNullWhen(returnValue: true, nameof(BackupFilePath))]
    public bool IsBackupFileAvailable => !string.IsNullOrWhiteSpace(BackupFilePath);
}