using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;

internal static class LoggerExtension {
    private static readonly Action<ILogger<SaveDocumentDirectoryRequestHandler>, Exception>
        CreateDocumentDirectoryFailedDelegate = LoggerMessage.Define(logLevel: LogLevel.Error,
                                                                          eventId: default,
                                                                          formatString: "An error occurred while creating a new document directory.",
                                                                          options: null);

    internal static void CreateDocumentDirectoryFailed(this ILogger<SaveDocumentDirectoryRequestHandler> self, Exception exception)
        => CreateDocumentDirectoryFailedDelegate(self, exception);

    private static readonly Action<ILogger<SaveDocumentDirectoryRequestHandler>, Guid, Exception>
        EditDocumentDirectoryFailedDelegate = LoggerMessage.Define<Guid>(logLevel: LogLevel.Error,
                                                                         eventId: default,
                                                                         formatString: "An error occurred while editing the document directory with ID '{DocumentDirectoryID}'.",
                                                                         options: null);

    internal static void EditDocumentDirectoryFailed(this ILogger<SaveDocumentDirectoryRequestHandler> self, Guid id, Exception exception)
        => EditDocumentDirectoryFailedDelegate(self, id, exception);
}
