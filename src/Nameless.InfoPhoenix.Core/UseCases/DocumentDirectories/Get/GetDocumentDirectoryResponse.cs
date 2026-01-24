using Nameless.InfoPhoenix.DTOs;
using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Get;

public class GetDocumentDirectoryResponse : Result<DocumentDirectoryDto> {
    private GetDocumentDirectoryResponse(DocumentDirectoryDto? value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator GetDocumentDirectoryResponse(DocumentDirectoryDto value) {
        return new GetDocumentDirectoryResponse(value, errors: []);
    }

    public static implicit operator GetDocumentDirectoryResponse(Error error) {
        return new GetDocumentDirectoryResponse(value: null, errors: [error]);
    }

    public static implicit operator GetDocumentDirectoryResponse(Error[] errors) {
        return new GetDocumentDirectoryResponse(value: null, errors);
    }
}