namespace Nameless.InfoPhoenix.Bootstrapping {
    public interface IStep {
        #region Properties

        string Name { get; }
        int Order { get; }
        bool ThrowOnFailure { get; }
        bool Skip { get; }

        #endregion

        #region Methods

        Task ExecuteAsync();

        #endregion
    }
}
