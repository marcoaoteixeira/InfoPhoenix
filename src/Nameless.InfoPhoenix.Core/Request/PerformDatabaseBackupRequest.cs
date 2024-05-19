using MediatR;
using Nameless.InfoPhoenix.Responses;

namespace Nameless.InfoPhoenix.Request {
    public sealed record PerformDatabaseBackupRequest : IRequest<EmptyResponse> {
    }
}
