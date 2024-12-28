using MediatR;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Notification;
using Nameless.Search;

namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public sealed class SearchDocumentsRequestHandler : IRequestHandler<SearchDocumentsRequest, SearchDocumentsResponse> {
    private readonly IIndexProvider _indexProvider;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SearchDocumentsRequestHandler> _logger;

    public SearchDocumentsRequestHandler(IIndexProvider indexProvider,
                                         INotificationService notificationService,
                                         ILogger<SearchDocumentsRequestHandler> logger) {
        _indexProvider = Prevent.Argument.Null(indexProvider);
        _notificationService = Prevent.Argument.Null(notificationService);
        _logger = Prevent.Argument.Null(logger);
    }

    public Task<SearchDocumentsResponse> Handle(SearchDocumentsRequest request, CancellationToken cancellationToken) {
        var result = new SearchDocumentsResponse();

        if (string.IsNullOrWhiteSpace(request.QueryTerm)) {
            return Task.FromResult(result);
        }

        try {
            var searchToken = CreateSearchToken(request.QueryTerm);
            var index = _indexProvider.CreateIndex(Constants.Common.IndexName);

            var builder = index.CreateSearchBuilder();

            ConfigureForExactToken(builder, searchToken.Exact);
            ConfigureForIncludeToken(builder, searchToken.Include);
            ConfigureForExcludeToken(builder, searchToken.Exclude);
            ConfigureSortBy(builder);

            var searchResult = builder.Search()
                                      .Select(SearchResult.From)
                                      .ToArray();

            result = new SearchDocumentsResponse {
                Value = new SearchResultCollection(searchResult),
                HighlightTerms = [.. searchToken.Exact, .. searchToken.Include]
            };

            return Task.FromResult(result);
        } catch (Exception ex) {
            _logger.ErrorOnExecuteLuceneSearch(ex);

            _notificationService.PublishAsync(new SearchDocumentErrorNotification {
                Title = "Pesquisa de Documentos",
                Message = $"Ocorreu um erro ao executar a pesquida de documentos. Erro: {ex.Message}",
                Type = NotificationType.Error
            });
        }

        return Task.FromResult(result);
    }

    private static SearchToken CreateSearchToken(string query) {
        // Let's get the tokens that will be used to EXACT MATCH SEARCH
        var matchCollection = RegexUtils.SearchToken().Matches(query);
        var exactTokens = matchCollection.Count > 0
            ? matchCollection.Select(item => item.Value).ToArray()
            : [];

        // Let's remove them from the original query
        query = exactTokens.Aggregate(seed: query,
                                      func: (current, exactToken) => current
                                                                     .Replace(exactToken, string.Empty)
                                                                     .Trim());

        // Let's break by space
        var tokens = query.Split(Constants.Separators.WHITE_SPACE)
                          .Where(token => !string.IsNullOrWhiteSpace(token))
                          .ToArray();

        var includeTokens = tokens.Where(token => !token.StartsWith(Separators.DASH))
                                  .ToArray();

        var excludeTokens = tokens.Where(token => token.StartsWith(Separators.DASH))
                                  .ToArray();

        return new SearchToken {
            Exact = exactTokens.Select(token => token.Replace(Constants.Common.DOUBLE_QUOTE, string.Empty))
                               .ToArray(),
            Include = includeTokens,
            Exclude = excludeTokens
        };
    }

    private static void ConfigureForExactToken(ISearchBuilder builder, string[] tokens) {
        foreach (var token in tokens) {
            builder
                .WithField(field: nameof(IndexFields.DocumentContent),
                           parts: token.ToLowerInvariant()
                                       .Split(Constants.Separators.WHITE_SPACE))
                .ExactMatch();
        }
    }

    private static void ConfigureForIncludeToken(ISearchBuilder builder, IEnumerable<string> tokens) {
        foreach (var token in tokens) {
            builder
                .WithField(field: nameof(IndexFields.DocumentContent),
                           value: token.ToLowerInvariant(),
                           useWildcard: token.Contains(Constants.Common.ASTERISK));
        }
    }

    private static void ConfigureForExcludeToken(ISearchBuilder builder, IEnumerable<string> tokens) {
        foreach (var token in tokens) {
            builder
                .WithField(field: nameof(IndexFields.DocumentContent),
                           value: token.ToLowerInvariant()
                                       .Replace(Separators.DASH, string.Empty),
                           useWildcard: false)
                .Forbidden();
        }
    }

    private static void ConfigureSortBy(ISearchBuilder builder)
        => builder
           .SortByString(nameof(IndexFields.DocumentDirectoryID))
           .Ascending();

    private record SearchToken {
        public string[] Include { get; init; } = [];
        public string[] Exclude { get; init; } = [];
        public string[] Exact { get; init; } = [];
    }
}