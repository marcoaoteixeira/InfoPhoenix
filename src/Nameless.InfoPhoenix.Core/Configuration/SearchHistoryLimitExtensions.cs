using Nameless.InfoPhoenix.Resources;

namespace Nameless.InfoPhoenix.Configuration;

public static class SearchHistoryLimitExtensions {
    extension(SearchHistoryLimit self) {
        public string GetDisplayText() {
            return self switch {
                SearchHistoryLimit.Small => Strings.SearchHistoryLimit_Small,
                SearchHistoryLimit.Medium => Strings.SearchHistoryLimit_Medium,
                SearchHistoryLimit.Large => Strings.SearchHistoryLimit_Large,
                _ => Strings.SearchHistoryLimit_None
            };
        }
    }
}