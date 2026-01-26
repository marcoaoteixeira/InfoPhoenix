using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

public readonly record struct CreateNewDocumentDirectoryMetadata(Guid DocumentDirectoryID);

public class CreateNewDocumentDirectoryResponse : Result<CreateNewDocumentDirectoryMetadata> {
    private CreateNewDocumentDirectoryResponse(CreateNewDocumentDirectoryMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator CreateNewDocumentDirectoryResponse(CreateNewDocumentDirectoryMetadata value) {
        return new CreateNewDocumentDirectoryResponse(value, errors: []);
    }

    public static implicit operator CreateNewDocumentDirectoryResponse(Error error) {
        return new CreateNewDocumentDirectoryResponse(value: default, errors: [error]);
    }

    public static implicit operator CreateNewDocumentDirectoryResponse(Error[] errors) {
        return new CreateNewDocumentDirectoryResponse(value: default, errors);
    }
}