using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.UpdateContent;
internal static class LoggerExtension {
    private static readonly Action<ILogger<UpdateDocumentDirectoryContentRequestHandler>, Guid, Exception?>
        StartingUpdateDocumentDirectoryContentDelegate = LoggerMessage.Define<Guid>(logLevel: LogLevel.Information,
                                                                                        eventId: default,
                                                                                        formatString: "Starting fetch documents from document directory. Document Directory ID: {DocumentDirectoryID}",
                                                                                        options: null);

    internal static void StartingUpdateDocumentDirectoryContent(this ILogger<UpdateDocumentDirectoryContentRequestHandler> self, Guid documentDirectoryID)
        => StartingUpdateDocumentDirectoryContentDelegate(self, documentDirectoryID, null /* exception */);

    private static readonly Action<ILogger<UpdateDocumentDirectoryContentRequestHandler>, Guid, Exception?>
        DocumentDirectoryNotFoundDelegate = LoggerMessage.Define<Guid>(logLevel: LogLevel.Warning,
                                                                       eventId: default,
                                                                       formatString: "Document directory not found in database. Document Directory ID: {DocumentDirectoryID}",
                                                                       options: null);

    internal static void DocumentDirectoryNotFound(this ILogger<UpdateDocumentDirectoryContentRequestHandler> self, Guid documentDirectoryID)
        => DocumentDirectoryNotFoundDelegate(self, documentDirectoryID, null /* exception */);

    private static readonly Action<ILogger<UpdateDocumentDirectoryContentRequestHandler>, string, string, Exception?>
        UnableToUpdateDocumentDirectoryContentDelegate = LoggerMessage.Define<string, string>(logLevel: LogLevel.Warning,
                                                                                  eventId: default,
                                                                                  formatString: "Unable to fetch document content. Document path: {DocumentFilePath}. Error: {Error}",
                                                                                  options: null);

    internal static void UnableToUpdateDocumentDirectoryContent(this ILogger<UpdateDocumentDirectoryContentRequestHandler> self, string filePath, string error)
        => UnableToUpdateDocumentDirectoryContentDelegate(self, filePath, error, null /* exception */);
}
