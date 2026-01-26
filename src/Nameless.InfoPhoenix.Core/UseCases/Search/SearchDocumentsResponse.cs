using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.Search;

public class SearchDocumentsResponse : Result<SearchDocumentsMetadata> {
    public static SearchDocumentsResponse Empty { get; } = new(
        value: new SearchDocumentsMetadata(
            SearchTerm: string.Empty,
            HighlightTerms: [],
            DocumentDirectories: [],
            Count: 0
        ),
        errors: []
    );

    private SearchDocumentsResponse(SearchDocumentsMetadata? value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator SearchDocumentsResponse(SearchDocumentsMetadata value) {
        return new SearchDocumentsResponse(value, errors: []);
    }

    public static implicit operator SearchDocumentsResponse(Error[] errors) {
        return new SearchDocumentsResponse(value: null, errors);
    }

    public static implicit operator SearchDocumentsResponse(Error error) {
        return new SearchDocumentsResponse(value: null, errors: [error]);
    }
}