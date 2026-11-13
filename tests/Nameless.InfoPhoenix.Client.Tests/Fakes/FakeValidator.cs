using Nameless.Validation;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IValidator"/> fake. Always reports success - the view models this
/// ticket touches use it only as a constructor dependency; the actual validation rules under
/// test are exercised directly against <c>DocumentDirectoryWindowViewModelValidator</c>
/// (a FluentValidation <c>AbstractValidator</c>), not through this interface.
/// </summary>
internal sealed class FakeValidator : IValidator {
    public Task<ValidationResult> ValidateAsync(object value, IDictionary<string, object> context, CancellationToken cancellationToken) {
        return Task.FromResult(ValidationResult.Successful);
    }
}