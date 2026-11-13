using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.Localization;

namespace Nameless.InfoPhoenix.Client.Infrastructure.Validations;

public class DocumentDirectoryWindowViewModelValidator : AbstractValidator<DocumentDirectoryWindowViewModel> {
    private readonly AppDbContext _dbContext;

    public DocumentDirectoryWindowViewModelValidator(AppDbContext dbContext, ITranslator<DocumentDirectoryWindowViewModelValidator> translator) {
        _dbContext = Throws.When.Null(dbContext);

        RuleFor(obj => obj.Label).NotEmpty()
                                 .WithMessage(translator["Label_Empty_Error_Message"]);

        RuleFor(obj => obj.DirectoryPath).Cascade(CascadeMode.Stop)
                                         .NotEmpty()
                                         .WithMessage(translator["DirectoryPath_Empty_Error_Message"])
                                         .MaximumLength(Conventions.Column.Size.Medium)
                                         .WithMessage(translator["DirectoryPath_MaximumLength_Error_Message"])
                                         .MustAsync(UniqueDirectoryPathAsync)
                                         .WithMessage(translator["DirectoryPath_Unique_Error_Message"]);

        RuleFor(obj => obj.DisplayPosition).LessThanOrEqualTo(valueToCompare: -1)
                                           .WithMessage(translator["DisplayPosition_LowerOrEqualTo_Error_Message"]);
    }

    private Task<bool> UniqueDirectoryPathAsync(DocumentDirectoryWindowViewModel obj, string directoryPath, CancellationToken cancellationToken) {
        return _dbContext.DocumentDirectories.AnyAsync(
            entity => entity.DirectoryPath == directoryPath,
            cancellationToken
        );
    }
}
