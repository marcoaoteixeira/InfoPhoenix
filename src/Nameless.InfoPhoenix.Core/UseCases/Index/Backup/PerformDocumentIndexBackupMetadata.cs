namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public readonly record struct PerformDocumentIndexBackupMetadata(string BackupFilePath) {
    public bool IsBackupAvailable => !string.IsNullOrWhiteSpace(BackupFilePath);
}