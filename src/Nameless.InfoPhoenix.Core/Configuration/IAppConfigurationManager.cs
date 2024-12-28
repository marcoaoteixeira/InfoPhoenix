namespace Nameless.InfoPhoenix.Configuration {
    public interface IAppConfigurationManager {
        Theme GetTheme();
        void SetTheme(Theme value);

        SearchHistoryLimit GetSearchHistoryLimit();
        void SetSearchHistoryLimit(SearchHistoryLimit value);

        string[] GetSearchHistory();
        void SetSearchHistory(string[] value);
        
        bool GetConfirmBeforeExit();
        void SetConfirmBeforeExit(bool value);

        bool GetEnableDocumentViewer();
        void SetEnableDocumentViewer(bool value);

        void Initialize();

        void CommitChanges();
    }
}
