using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Configuration;

namespace Nameless.InfoPhoenix.Infrastructure.Configuration;

public sealed class AppConfigurationManager : IAppConfigurationManager, IDisposable {
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<AppConfigurationManager> _logger;

    private Theme _theme;
    private SearchHistoryLimit _searchHistoryLimit;
    private string[] _searchHistory = [];
    private bool _confirmBeforeExit;
    private bool _enableDocumentViewer;

    private bool _initialized;
    private bool _disposed;

    public AppConfigurationManager(IFileProvider fileProvider,
                                   ILogger<AppConfigurationManager> logger) {
        _fileProvider = Prevent.Argument.Null(fileProvider);
        _logger = Prevent.Argument.Null(logger);
    }

    public Theme GetTheme()
        => _theme;

    public void SetTheme(Theme value)
        => _theme = value;

    public SearchHistoryLimit GetSearchHistoryLimit()
        => _searchHistoryLimit == SearchHistoryLimit.None
            ? SearchHistoryLimit.Small
            : _searchHistoryLimit;

    public void SetSearchHistoryLimit(SearchHistoryLimit value)
        => _searchHistoryLimit = value == SearchHistoryLimit.None
            ? SearchHistoryLimit.Small
            : value;

    public string[] GetSearchHistory()
        => _searchHistory;

    public void SetSearchHistory(string[] value)
        => _searchHistory = Prevent.Argument.Null(value);

    public bool GetConfirmBeforeExit()
        => _confirmBeforeExit;

    public void SetConfirmBeforeExit(bool value)
        => _confirmBeforeExit = value;

    public bool GetEnableDocumentViewer()
        => _enableDocumentViewer;

    public void SetEnableDocumentViewer(bool value)
        => _enableDocumentViewer = value;

    public void Initialize() {
        if (_initialized) { return; }

        var appConfiguration = FetchFromFileSystem();

        _theme = appConfiguration.Theme;
        _searchHistoryLimit = appConfiguration.SearchHistoryLimit;
        _searchHistory = appConfiguration.SearchHistory;
        _confirmBeforeExit = appConfiguration.ConfirmBeforeExit;
        _enableDocumentViewer = appConfiguration.EnableDocumentViewer;

        _initialized = true;
    }

    public void CommitChanges() {
        var file = _fileProvider.GetFileInfo(Constants.APP_CONFIGURATION_FILE_NAME);
        var appConfiguration = GetCurrentAppConfiguration();
        var json = JsonSerializer.Serialize(appConfiguration);

        if (file.PhysicalPath is null) {
            _logger.AppConfigurationMissingPhysicalFilePath();

            return;
        }

        using var fileStream = new FileStream(file.PhysicalPath, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(fileStream);
        streamWriter.Write(json);
    }

    public void Dispose() {
        if (_disposed) { return; }

        CommitChanges();

        _disposed = true;
    }

    private AppConfiguration FetchFromFileSystem() {
        var file = _fileProvider.GetFileInfo(Constants.APP_CONFIGURATION_FILE_NAME);

        if (!file.Exists) {
            return AppConfiguration.Default;
        }

        using var stream = file.CreateReadStream();
        var content = stream.ToText();

        return JsonSerializer.Deserialize<AppConfiguration>(content)
               ?? AppConfiguration.Default;
    }

    private AppConfiguration GetCurrentAppConfiguration()
        => new(_theme,
               _searchHistoryLimit,
               _searchHistory,
               _confirmBeforeExit,
               _enableDocumentViewer);
}