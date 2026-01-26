using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public class PerformDocumentIndexBackupRequest : IRequest<PerformDocumentIndexBackupResponse> {
    public required DateTimeOffset BackupDate { get; init; }
}