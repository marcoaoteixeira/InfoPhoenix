namespace Nameless.InfoPhoenix.Objects {
    public abstract record Notification {
        #region Public Properties

        public Severity Severity { get; init; }
        public string Message { get; init; } = string.Empty;

        #endregion
    }

    public enum Severity {
        None,
        Information,
        Error,
        Success,
        Warning
    }
}
