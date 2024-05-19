namespace Nameless.InfoPhoenix.Responses {
    public abstract record MultipleValueResponse<TValue> : ResponseBase {
        #region Private Read-Only Fields

        private readonly TValue[] _value = [];

        #endregion
        #region Public Properties

        public TValue[] Value {
            get => _value;
            init => _value = value ?? [];
        }

        #endregion
    }
}
