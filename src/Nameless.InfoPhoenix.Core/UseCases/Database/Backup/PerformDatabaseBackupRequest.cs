using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

public record PerformDatabaseBackupRequest : IRequest<PerformDatabaseBackupResponse> {
    public required DateTimeOffset BackupDate { get; init; }
}