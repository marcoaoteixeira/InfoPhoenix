using Nameless.InfoPhoenix.DTOs;
using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.List;

public class ListDocumentDirectoriesResponse : Result<DocumentDirectoryDto[]> {
    private ListDocumentDirectoriesResponse(DocumentDirectoryDto[] value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator ListDocumentDirectoriesResponse(DocumentDirectoryDto[] value) {
        return new ListDocumentDirectoriesResponse(value, errors: []);
    }

    public static implicit operator ListDocumentDirectoriesResponse(Error error) {
        return new ListDocumentDirectoriesResponse(value: [], errors: [error]);
    }

    public static implicit operator ListDocumentDirectoriesResponse(Error[] errors) {
        return new ListDocumentDirectoriesResponse(value: [], errors);
    }
}