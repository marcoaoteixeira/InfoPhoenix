namespace Nameless.InfoPhoenix.Responses {
    public abstract record SingleValueResponse<TValue> : ResponseBase {
        #region Public Properties

        public TValue? Value { get; init; }

        #endregion
    }
}
