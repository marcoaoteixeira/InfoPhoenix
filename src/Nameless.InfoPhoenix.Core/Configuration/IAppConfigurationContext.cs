namespace Nameless.InfoPhoenix.Configuration {
    public interface IAppConfigurationContext {
        #region Properties

        string Theme { get; set; }
        int SearchHistoryLimit { get; set; }
        ISet<string> SearchHistory { get; }
        bool ConfirmBeforeExit { get; set; }
        bool EnableDocumentViewer { get; set; }

        #endregion
    }
}
