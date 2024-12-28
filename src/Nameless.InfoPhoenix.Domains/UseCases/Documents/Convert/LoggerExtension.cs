using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Domains.UseCases.Documents.Convert;

internal static class LoggerExtension {
    private static readonly Action<ILogger<ConvertDocumentToXpsFileRequestHandler>, Exception>
        ErrorWhileWritingTempFileDelegate = LoggerMessage.Define(logLevel: LogLevel.Error,
                                                                 eventId: default,
                                                                 formatString: "An error occurred while writing the converted document file.",
                                                                 options: null);

    internal static void ErrorWhileWritingTempFile(this ILogger<ConvertDocumentToXpsFileRequestHandler> self, Exception exception)
        => ErrorWhileWritingTempFileDelegate(self, exception);
}