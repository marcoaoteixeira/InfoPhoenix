using Nameless.InfoPhoenix.Configuration;

namespace Nameless.InfoPhoenix.Infrastructure.Configuration;

public sealed record AppConfiguration(Theme Theme,
                                      SearchHistoryLimit SearchHistoryLimit,
                                      string[] SearchHistory,
                                      bool ConfirmBeforeExit,
                                      bool EnableDocumentViewer) {
    public static AppConfiguration Default { get; } = new(Theme.Dark,
                                                          SearchHistoryLimit.Small,
                                                          SearchHistory: [],
                                                          ConfirmBeforeExit: true,
                                                          EnableDocumentViewer: false);
}