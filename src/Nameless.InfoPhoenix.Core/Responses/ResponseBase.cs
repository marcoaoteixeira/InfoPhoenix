using System.Diagnostics.CodeAnalysis;

namespace Nameless.InfoPhoenix.Responses {
    public abstract record ResponseBase {
        #region Public Properties

        public string? Error { get; init; }

        [MemberNotNullWhen(false, nameof(Error))]
        public bool Succeeded()
            => string.IsNullOrWhiteSpace(Error);

        #endregion
    }
}
