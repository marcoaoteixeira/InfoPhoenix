using System.IO;
using FluentValidation;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;

public class UpdateDocumentDirectoryRequestValidator : AbstractValidator<UpdateDocumentDirectoryRequest> {
    public UpdateDocumentDirectoryRequestValidator() {
        RuleFor(request => request.DocumentDirectoryID).NotEqual(Guid.Empty)
                                                       .WithMessage("É necessário prover um ID válido para o diretório de documentos.");

        RuleFor(request => request.Label).NotEmpty()
                                         .WithMessage("O campo 'Rótulo' deve ser preenchido.");

        RuleFor(request => request.DirectoryPath).NotEmpty()
                                                 .WithMessage("O campo 'Caminho do Diretório' deve ser preenchido.")
                                                 .Must(Directory.Exists)
                                                 .WithMessage("O diretório especificado não existe.");

        RuleFor(request => request.DisplayPosition).GreaterThanOrEqualTo(valueToCompare: 0)
                                                   .WithMessage("O valor de 'Posição' não pode ser inferior a zero.");
    }
}