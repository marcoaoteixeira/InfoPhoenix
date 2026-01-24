using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Documents;

internal static class LoggerExtensions {
    private static readonly Action<ILogger, string, Exception> GetContentFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to read the document '{DocumentFilePath}'."
        );

    private static readonly Action<ILogger, string, Exception> XpsConvertFailureDelegate
        = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: default,
            formatString: "An error occurred while trying to convert the document '{DocumentFilePath}' to XPS file."
        );

    extension(ILogger<IDocumentReader> self) {
        internal void GetContentFailure(string filePath, Exception exception) {
            GetContentFailureDelegate(self, filePath, exception);
        }
    }

    extension(ILogger<XpsDocumentConverter> self) {
        internal void ConvertFailure(string filePath, Exception exception) {
            XpsConvertFailureDelegate(self, filePath, exception);
        }
    }
}
