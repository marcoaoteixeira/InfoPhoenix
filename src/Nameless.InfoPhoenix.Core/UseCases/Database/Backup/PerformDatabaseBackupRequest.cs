using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

public record PerformDatabaseBackupRequest : IRequest<PerformDatabaseBackupResponse>;