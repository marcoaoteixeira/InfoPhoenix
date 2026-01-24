using Moq;
using Nameless.Testing.Tools.Mockers;
using Nameless.Validation;

namespace Nameless.InfoPhoenix.Testing.Tools.Mockers.Core;

public class ValidationServiceMocker : Mocker<IValidationService> {
    public ValidationServiceMocker WithValidateAsync(ValidationResult returnValue) {
        MockInstance
            .Setup(mock => mock.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnValue);

        return this;
    }
}
