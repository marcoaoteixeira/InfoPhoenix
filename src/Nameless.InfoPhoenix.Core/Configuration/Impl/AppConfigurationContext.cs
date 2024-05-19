using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace Nameless.InfoPhoenix.Configuration.Impl {
    public sealed class AppConfigurationContext : IAppConfigurationContext, IDisposable {
        #region Public Constants

        public const string APP_CONFIGURATION_FILE_NAME = "info_phoenix.config";

        #endregion

        #region Private Static Read-Only Properties

        private static AppConfiguration AppConfigurationDefault { get; } = new(
            Theme: Root.Defaults.APPLICATION_THEME,
            SearchHistoryLimit: 10,
            SearchHistory: [],
            ConfirmBeforeExit: false);

        #endregion

        #region Private Read-Only Fields

        private readonly IFileProvider _fileProvider;

        #endregion

        #region Private Fields

        private string? _theme;
        private bool _disposed;

        #endregion

        #region Public Constructors

        public AppConfigurationContext(IFileProvider fileProvider) {
            _fileProvider = Guard.Against.Null(fileProvider, nameof(fileProvider));

            Initialize();
        }

        #endregion

        #region Destructor

        ~AppConfigurationContext() {
            Dispose(disposing: false);
        }

        #endregion

        #region Private Methods

        private void Initialize() {
            var appConfiguration = FetchFromFileSystem();

            Theme = appConfiguration.Theme;
            SearchHistoryLimit = appConfiguration.SearchHistoryLimit;
            SearchHistory = new HashSet<string>(appConfiguration.SearchHistory);
            ConfirmBeforeExit = appConfiguration.ConfirmBeforeExit;
        }

        private AppConfiguration FetchFromFileSystem() {
            var file = _fileProvider.GetFileInfo(APP_CONFIGURATION_FILE_NAME);

            if (!file.Exists) {
                return AppConfigurationDefault;
            }

            using var stream = file.CreateReadStream();
            var content = stream.ToText();

            return JsonSerializer.Deserialize<AppConfiguration>(content)
                ?? AppConfigurationDefault;
        }

        private AppConfiguration GetCurrentAppConfiguration()
            => new(Theme,
                   SearchHistoryLimit,
                   [.. SearchHistory.Reverse().Take(10)],
                   ConfirmBeforeExit);

        private void StoreIntoFileSystem() {
            var file = _fileProvider.GetFileInfo(APP_CONFIGURATION_FILE_NAME);
            var json = JsonSerializer.Serialize(GetCurrentAppConfiguration());

            using var fileStream = new FileStream(file.PhysicalPath!, FileMode.Create, FileAccess.Write);
            using var streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(json);
        }

        private void Dispose(bool disposing) {
            if (_disposed) { return; }

            if (disposing) {
                StoreIntoFileSystem();
            }

            _disposed = true;
        }

        #endregion

        #region Public Records

        // Record used to persist configurations in JSON format.
        public sealed record AppConfiguration(string Theme, int SearchHistoryLimit, string[] SearchHistory, bool ConfirmBeforeExit);

        #endregion

        #region IAppConfigurationContext Members

        public string Theme {
            get => _theme ?? Root.Defaults.APPLICATION_THEME;
            set => _theme = string.IsNullOrWhiteSpace(value)
                ? Root.Defaults.APPLICATION_THEME
                : value;
        }

        public int SearchHistoryLimit { get; set; }

        public ISet<string> SearchHistory { get; private set; } = new HashSet<string>();

        public bool ConfirmBeforeExit { get; set; }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
