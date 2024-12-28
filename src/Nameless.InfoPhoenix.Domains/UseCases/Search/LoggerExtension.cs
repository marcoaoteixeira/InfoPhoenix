using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

internal static class LoggerExtension {
    private static readonly Action<ILogger<SearchDocumentsRequestHandler>, Exception>
        ErrorOnExecuteLuceneSearchDelegate = LoggerMessage.Define(logLevel: LogLevel.Error,
                                                                  eventId: default,
                                                                  formatString: "",
                                                                  options: null);

    internal static void ErrorOnExecuteLuceneSearch(this ILogger<SearchDocumentsRequestHandler> self, Exception exception)
        => ErrorOnExecuteLuceneSearchDelegate(self, exception);
}
