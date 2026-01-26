using System.IO;
using FluentValidation;

namespace Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;

public class CreateNewDocumentDirectoryRequestValidator : AbstractValidator<CreateNewDocumentDirectoryRequest> {
    public CreateNewDocumentDirectoryRequestValidator() {
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