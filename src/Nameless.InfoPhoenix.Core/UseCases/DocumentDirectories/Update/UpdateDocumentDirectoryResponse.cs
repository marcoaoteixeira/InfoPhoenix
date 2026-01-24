using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

public readonly record struct UpdateDocumentDirectoryMetadata(Guid DocumentDirectoryID);

public class UpdateDocumentDirectoryResponse : Result<UpdateDocumentDirectoryMetadata> {
    private UpdateDocumentDirectoryResponse(UpdateDocumentDirectoryMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator UpdateDocumentDirectoryResponse(UpdateDocumentDirectoryMetadata value) {
        return new UpdateDocumentDirectoryResponse(value, errors: []);
    }

    public static implicit operator UpdateDocumentDirectoryResponse(Error error) {
        return new UpdateDocumentDirectoryResponse(value: default, errors: [error]);
    }

    public static implicit operator UpdateDocumentDirectoryResponse(Error[] errors) {
        return new UpdateDocumentDirectoryResponse(value: default, errors);
    }
}