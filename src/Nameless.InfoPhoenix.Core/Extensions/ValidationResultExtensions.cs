using Nameless.ObjectModel;
using Nameless.Validation;

namespace Nameless.InfoPhoenix;

public static class ValidationResultExtensions {
    extension(ValidationResult self) {
        public Error[] AsErrors() {
            if (self.Success) { return []; }

            return [..
                self.Errors
                    .Select(error
                        => Error.Validation(error.Error, error.MemberName)
                    )
            ];
        }
    }
}
