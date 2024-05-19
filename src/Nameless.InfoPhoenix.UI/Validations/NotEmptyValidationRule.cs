using System.Globalization;
using System.Windows.Controls;

namespace Nameless.InfoPhoenix.UI.Validations {
    public class NotEmptyValidationRule : ValidationRule {
        #region Public Override Methods

        public override ValidationResult Validate(object? value, CultureInfo cultureInfo) {
            if (value is string text && string.IsNullOrWhiteSpace(text)) {
                return new ValidationResult(
                    isValid: false,
                    errorContent: "Valor não pode ser vazio ou espaços em branco."
                );
            }

            return ValidationResult.ValidResult;
        }

        #endregion
    }
}
