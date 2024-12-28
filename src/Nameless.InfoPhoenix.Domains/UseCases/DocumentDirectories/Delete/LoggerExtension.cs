using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Delete;

internal static class LoggerExtension {
    private static readonly Action<ILogger<DeleteDocumentDirectoryRequestHandler>, Exception>
        DeleteDocumentDirectoryFailedDelegate = LoggerMessage.Define(logLevel: LogLevel.Error,
                                                                     eventId: default,
                                                                     formatString: "An error occurred while deleting the document directory.",
                                                                     options: null);

    internal static void DeleteDocumentDirectoryFailed(this ILogger<DeleteDocumentDirectoryRequestHandler> self, Exception exception)
        => DeleteDocumentDirectoryFailedDelegate(self, exception);
}
