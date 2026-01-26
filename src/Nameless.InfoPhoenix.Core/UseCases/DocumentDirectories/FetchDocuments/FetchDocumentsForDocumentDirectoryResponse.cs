using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

public readonly record struct FetchDocumentsForDocumentDirectoryMetadata(int DocumentCount);

public class FetchDocumentsForDocumentDirectoryResponse : Result<FetchDocumentsForDocumentDirectoryMetadata> {
    private FetchDocumentsForDocumentDirectoryResponse(FetchDocumentsForDocumentDirectoryMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator FetchDocumentsForDocumentDirectoryResponse(FetchDocumentsForDocumentDirectoryMetadata value) {
        return new FetchDocumentsForDocumentDirectoryResponse(value, errors: []);
    }

    public static implicit operator FetchDocumentsForDocumentDirectoryResponse(Error error) {
        return new FetchDocumentsForDocumentDirectoryResponse(value: default, errors: [error]);
    }

    public static implicit operator FetchDocumentsForDocumentDirectoryResponse(Error[] errors) {
        return new FetchDocumentsForDocumentDirectoryResponse(value: default, errors);
    }
}