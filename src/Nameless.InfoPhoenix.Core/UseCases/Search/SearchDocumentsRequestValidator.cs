using FluentValidation;

namespace Nameless.InfoPhoenix.UseCases.Search;

public class SearchDocumentsRequestValidator : AbstractValidator<SearchDocumentsRequest> {
    public SearchDocumentsRequestValidator() {
        RuleFor(request => request.Term)
            .NotEmpty()
            .WithMessage("Search term must be provided.");
    }
}