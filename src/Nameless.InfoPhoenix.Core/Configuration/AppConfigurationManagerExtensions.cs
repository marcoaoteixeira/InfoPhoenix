using Nameless.WPF.Configuration;

namespace Nameless.InfoPhoenix.Configuration;

public static class AppConfigurationManagerExtensions {
    private const string SEARCH_HISTORY_LIMIT_KEY = nameof(SearchHistoryLimit);
    private const string SEARCH_HISTORY_KEY = "SearchHistory";
    private const string ENABLE_DOCUMENT_VIEWER_KEY = "EnableDocumentViewer";

    extension(IAppConfigurationManager self) {
        public SearchHistoryLimit GetSearchHistoryLimit() {
            return self.TryGet<SearchHistoryLimit>(SEARCH_HISTORY_LIMIT_KEY, out var output) ? output : default;
        }

        public void SetSearchHistoryLimit(SearchHistoryLimit value) {
            self.Set(SEARCH_HISTORY_LIMIT_KEY, value);
        }

        public string[] GetSearchHistory() {
            return self.TryGet<string[]>(SEARCH_HISTORY_KEY, out var output) ? output : [];
        }

        public void SetSearchHistory(string[] value) {
            self.Set(SEARCH_HISTORY_KEY, value);
        }

        public bool GetEnableDocumentViewer() {
            return self.TryGet<bool>(ENABLE_DOCUMENT_VIEWER_KEY, out var output) && output;
        }

        public void SetEnableDocumentViewer(bool value) {
            self.Set(ENABLE_DOCUMENT_VIEWER_KEY, value);
        }
    }
}
