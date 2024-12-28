using MediatR;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Domains.UseCases.Database.PerformBackup;

public sealed record PerformDatabaseBackupRequest : IRequest<Result<string>>;