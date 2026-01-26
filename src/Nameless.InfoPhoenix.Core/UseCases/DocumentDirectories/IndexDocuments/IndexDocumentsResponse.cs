using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;

public readonly record struct IndexDocumentsMetadata(
    int TotalDocuments,
    int TotalIndexed
);

public class IndexDocumentsResponse : Result<IndexDocumentsMetadata> {
    private IndexDocumentsResponse(IndexDocumentsMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator IndexDocumentsResponse(IndexDocumentsMetadata value) {
        return new IndexDocumentsResponse(value, errors: []);
    }

    public static implicit operator IndexDocumentsResponse(Error error) {
        return new IndexDocumentsResponse(value: default, errors: [error]);
    }

    public static implicit operator IndexDocumentsResponse(Error[] errors) {
        return new IndexDocumentsResponse(value: default, errors);
    }
}