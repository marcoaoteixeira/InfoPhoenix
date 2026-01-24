using Nameless.InfoPhoenix.Internals;

namespace Nameless.InfoPhoenix.UseCases.Search;

public static class ContentSearchTokensParser {
    public static ContentSearchTokens Parse(string query) {
        var normalizedQuery = query.ToLowerInvariant().Trim();

        normalizedQuery = ExtractExactMatches(normalizedQuery, out var exact);
        normalizedQuery = ExtractIncludeMatches(normalizedQuery, out var includes);
        normalizedQuery = ExtractExcludeMatches(normalizedQuery, out var excludes);

        return new ContentSearchTokens(includes, excludes, exact);
    }

    private static string ExtractExactMatches(string query, out string[] values) {
        // Let's get the tokens that will be used to EXACT MATCH SEARCH
        var matchCollection = RegexCache.ContentSearchTokenPattern().Matches(query);

        values = matchCollection.Count > 0
            ? [.. matchCollection.Select(item => item.Value)]
            : [];

        return Cleanup(query, values);
    }

    private static string ExtractIncludeMatches(string query, out string[] values) {
        // Let's break by space
        var tokens = query.Split(Separators.SPACE)
                          .Where(token => !string.IsNullOrWhiteSpace(token))
                          .ToArray();

        values = [.. tokens.Where(token => !token.StartsWith(Separators.DASH))];

        return Cleanup(query, values);
    }

    private static string ExtractExcludeMatches(string query, out string[] values) {
        // Let's break by space
        var tokens = query.Split(Separators.SPACE)
                          .Where(token => !string.IsNullOrWhiteSpace(token))
                          .ToArray();

        values = [.. tokens.Where(token => token.StartsWith(Separators.DASH))];

        return Cleanup(query, values);
    }

    // Removes the specified tokens from the query string
    private static string Cleanup(string query, string[] remove) {
        return remove.Aggregate(
            seed: query,
            func: (current, exactToken) => current.Replace(exactToken, string.Empty)
                                                  .Trim()
                                                  .ToLowerInvariant()
        );
    }
}
