using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

public readonly record struct DeleteDocumentDirectoryMetadata(Guid DocumentDirectoryID);

public class DeleteDocumentDirectoryResponse : Result<DeleteDocumentDirectoryMetadata> {
    private DeleteDocumentDirectoryResponse(DeleteDocumentDirectoryMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator DeleteDocumentDirectoryResponse(DeleteDocumentDirectoryMetadata value) {
        return new DeleteDocumentDirectoryResponse(value, errors: []);
    }

    public static implicit operator DeleteDocumentDirectoryResponse(Error error) {
        return new DeleteDocumentDirectoryResponse(value: default, errors: [error]);
    }

    public static implicit operator DeleteDocumentDirectoryResponse(Error[] errors) {
        return new DeleteDocumentDirectoryResponse(value: default, errors);
    }
}