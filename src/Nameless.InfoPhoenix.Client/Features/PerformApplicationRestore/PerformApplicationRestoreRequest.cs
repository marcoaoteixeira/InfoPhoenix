using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;

/// <summary>Restores a previously created backup archive (plan IPX-0011 §3.1).</summary>
/// <param name="SourceZipFilePath">The full path of the zip file the user picked (IPX-0010).</param>
public sealed record PerformApplicationRestoreRequest(string SourceZipFilePath) : IRequest<RestoreOutcome>;