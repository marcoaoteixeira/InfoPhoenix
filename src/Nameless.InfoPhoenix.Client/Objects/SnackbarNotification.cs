using Nameless.InfoPhoenix.Objects;

namespace Nameless.InfoPhoenix.Client.Objects {
    public sealed record SnackbarNotification : Notification {
        #region Public Properties

        public string? Title { get; init; }

        #endregion
    }
}
