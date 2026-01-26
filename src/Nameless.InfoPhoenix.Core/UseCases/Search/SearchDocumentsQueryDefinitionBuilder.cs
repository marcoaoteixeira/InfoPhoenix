using Nameless.Lucene;

namespace Nameless.InfoPhoenix.UseCases.Search;

public sealed class SearchDocumentsQueryDefinitionBuilder {
    private readonly IQueryBuilder _builder;

    private SearchDocumentsQueryDefinitionBuilder(IIndex index) {
        _builder = index.CreateQueryBuilder();
    }

    public static SearchDocumentsQueryDefinitionBuilder Create(IIndex index) {
        return new SearchDocumentsQueryDefinitionBuilder(index);
    }

    public SearchDocumentsQueryDefinitionBuilder UseDocumentDirectories(Guid[] ids) {
        _builder
            .WithField(
                name: nameof(Constants.Index.Fields.DocumentDirectoryID),
                values: [.. ids.Select(id => id.ToString())]
            )
            .ExactMatch();

        return this;
    }

    public SearchDocumentsQueryDefinitionBuilder UseExactTerms(string[] tokens) {
        foreach (var token in tokens) {
            _builder
                .WithField(
                    name: nameof(Constants.Index.Fields.Content),
                    values: token.Split(Separators.SPACE)
                )
                .ExactMatch();
        }

        return this;
    }

    public SearchDocumentsQueryDefinitionBuilder UseIncludeTerms(string[] tokens) {
        foreach (var token in tokens) {
            _builder
                .WithField(
                    name: nameof(Constants.Index.Fields.Content),
                    value: token,
                    useWildcard: token.Contains(Constants.Index.SearchWildcard)
                );
        }

        return this;
    }

    public SearchDocumentsQueryDefinitionBuilder UseExcludeTerms(string[] tokens) {
        foreach (var token in tokens) {
            _builder
                .WithField(
                    name: nameof(Constants.Index.Fields.Content),
                    value: token.Replace(Separators.DASH, string.Empty),
                    useWildcard: false)
                .Forbidden();
        }

        return this;
    }

    public SearchDocumentsQueryDefinitionBuilder UseSortBy(string field) {
        _builder.SortByString(field).Ascending();

        return this;
    }

    public SearchDocumentsQueryDefinitionBuilder UseSlice(int start, int limit) {
        _builder.Slice(start, limit);

        return this;
    }

    public QueryDefinition Build() {
        return _builder.Build();
    }
}
