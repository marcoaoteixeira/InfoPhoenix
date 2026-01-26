using FluentValidation;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;

public class FetchDocumentsForDocumentDirectoryRequestValidator : AbstractValidator<FetchDocumentsForDocumentDirectoryRequest> {
    public FetchDocumentsForDocumentDirectoryRequestValidator() {
        RuleFor(request => request.DocumentDirectoryID).NotEqual(Guid.Empty)
                                                       .WithMessage("É necessário prover um ID válido para o diretório de documentos.");
    }
}