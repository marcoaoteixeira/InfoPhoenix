using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Domain.Responses;
using Nameless.Lucene;

namespace Nameless.InfoPhoenix.Domain.Handlers {
    public sealed class ExecuteSearchRequestHandler : IRequestHandler<ExecuteSearchRequest, SearchResultEntryGroupCollectionResponse> {
        #region Private Read-Only Fields

        private readonly IIndexManager _indexManager;
        private readonly ILogger _logger;

        #endregion

        #region Public Constructors

        public ExecuteSearchRequestHandler(IIndexManager indexManager, ILogger<ExecuteSearchRequestHandler> logger) {
            _indexManager = Guard.Against.Null(indexManager, nameof(indexManager));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        #endregion

        #region IRequestHandler<ExecuteSearchRequest, SearchResultEntryCollectionResponse> Members

        public Task<SearchResultEntryGroupCollectionResponse> Handle(ExecuteSearchRequest request, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(request.Query)) {
                return Task.FromResult(new SearchResultEntryGroupCollectionResponse {
                    Error = "Necessário termo de pesquisa."
                });
            }

            var searchToken = CreateSearchToken(request.Query);
            var index = _indexManager.GetOrCreate(Root.Index.NAME);
            var builder = index.CreateSearchBuilder();

            ConfigureForExactToken(builder, searchToken.Exact);
            ConfigureForIncludeToken(builder, searchToken.Include);
            ConfigureForExcludeToken(builder, searchToken.Exclude);
            ConfigureSortBy(builder);

            var collection = Array.Empty<SearchResultEntryDto>();

            try {
                collection = builder.Search()
                                    .Select(searchHit => searchHit.ToSearchResultEntry())
                                    .ToArray();
            }
            catch (Exception ex) {
                _logger.LogError(exception: ex,
                                 message: "Error while searching index. Query: {Query}",
                                 args: request.Query
                );
            }

            var groups = collection
                .GroupBy(entry => (entry.DocumentDirectoryLabel, entry.DocumentDirectoryPath, entry.DocumentDirectoryLastIndexingTime))
                .Select(group => new SearchResultCollectionDto([.. group]) {
                    DocumentDirectoryLabel = group.Key.DocumentDirectoryLabel,
                    DocumentDirectoryPath = group.Key.DocumentDirectoryPath,
                    DocumentDirectoryLastIndexingTime = group.Key.DocumentDirectoryLastIndexingTime,
                })
                .ToArray();

            var response = new SearchResultEntryGroupCollectionResponse() {
                Value = [.. groups],
                Error = groups.Length == 0
                    ? $"Não há resultados para a busca. Termo utilizado: {request.Query}"
                    : null,
                HighlightTerms = [.. searchToken.Exact, .. searchToken.Include]
            };

            return Task.FromResult(response);
        }

        #endregion

        #region Private Static Methods

        private static SearchToken CreateSearchToken(string query) {
            // Let's get the tokens that will be used to EXACT MATCH SEARCH
            var matchCollection = Regex.Matches(input: query, pattern: "(\".*?\")");
            var exactTokens = matchCollection.Count > 0
                ? matchCollection.Select(item => item.Value).ToArray()
                : [];

            // Let's remove them from the original query
            query = exactTokens.Aggregate(seed: query,
                                          func: (current, exactToken)
                                              => current
                                                  .Replace(exactToken, string.Empty)
                                                  .Trim()
            );

            // Let's break by space
            var tokens = query
                .Split(Root.Defaults.Chars.WHITE_SPACE)
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToArray();

            var includeTokens = tokens
                                .Where(token => !token.StartsWith(Root.Defaults.Chars.DASH))
                                .ToArray();

            var excludeTokens = tokens
                                .Where(token => token.StartsWith(Root.Defaults.Chars.DASH))
                                .ToArray();

            return new SearchToken {
                Exact = exactTokens
                    .Select(token => token.Replace(Root.Defaults.Strings.DOUBLE_QUOTE_CHAR, string.Empty))
                    .ToArray(),
                Include = includeTokens,
                Exclude = excludeTokens
            };
        }

        private static void ConfigureForExactToken(ISearchBuilder builder, string[] tokens) {
            foreach (var token in tokens) {
                builder
                    .WithField(field: nameof(SearchResultEntryDto.DocumentContent),
                               phraseParts: token.ToLowerInvariant()
                                                 .Split(Root.Defaults.Chars.WHITE_SPACE))
                    .ExactMatch();
            }
        }

        private static void ConfigureForIncludeToken(ISearchBuilder builder, IEnumerable<string> tokens) {
            foreach (var token in tokens) {
                builder
                    .WithField(field: nameof(SearchResultEntryDto.DocumentContent),
                               value: token.ToLowerInvariant(),
                               useWildcard: token.Contains(Root.Defaults.Strings.ASTERISK));
            }
        }

        private static void ConfigureForExcludeToken(ISearchBuilder builder, IEnumerable<string> tokens) {
            foreach (var token in tokens) {
                builder
                    .WithField(field: nameof(SearchResultEntryDto.DocumentContent),
                               value: token.ToLowerInvariant()
                                           .Replace(Root.Defaults.Strings.DASH, string.Empty),
                               useWildcard: false)
                    .Forbidden();
            }
        }

        private static void ConfigureSortBy(ISearchBuilder builder)
            => builder
               .SortByString(nameof(SearchResultEntryDto.DocumentFileName))
               .Ascending();

        #endregion

        #region Private Inner Records

        private record SearchToken {
            #region Public Properties

            public string[] Include { get; init; } = [];
            public string[] Exclude { get; init; } = [];
            public string[] Exact { get; init; } = [];

            #endregion
        }

        #endregion
    }
}
