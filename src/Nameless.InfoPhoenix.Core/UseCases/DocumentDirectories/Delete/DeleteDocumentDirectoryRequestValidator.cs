using FluentValidation;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;

public class DeleteDocumentDirectoryRequestValidator : AbstractValidator<DeleteDocumentDirectoryRequest> {
    public DeleteDocumentDirectoryRequestValidator() {
        RuleFor(request => request.DocumentDirectoryID).NotEqual(Guid.Empty)
                                                       .WithMessage("É necessário prover um ID válido para o diretório de documentos.");
    }
}