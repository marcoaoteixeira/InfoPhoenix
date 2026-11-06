namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;

/// <summary>
/// Thrown by the restore handler's validation helpers (plan §3.1) to unwind to the single catch
/// that turns a <see cref="BackupRejection"/> into exactly one Failure entry and
/// <see cref="RestoreOutcome.Blocked"/>.
/// </summary>
internal sealed class BackupRejectedException(BackupRejection rejection) : Exception(rejection.ReasonKey) {
    internal BackupRejection Rejection { get; } = rejection;
}
