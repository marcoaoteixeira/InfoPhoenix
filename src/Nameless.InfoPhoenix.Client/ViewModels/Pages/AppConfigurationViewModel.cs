using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Objects;
using Nameless.InfoPhoenix.Request;
using Nameless.InfoPhoenix.UI;
using Nameless.InfoPhoenix.UI.Helpers;
using Nameless.Infrastructure;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using CoreRoot = Nameless.InfoPhoenix.Root;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages {
    public partial class AppConfigurationViewModel : ViewModelBase, INavigationAware {
        #region Private Read-Only Fields

        private readonly IApplicationContext _applicationContext;
        private readonly IAppConfigurationContext _appConfigurationContext;
        private readonly IMediator _mediator;

        #endregion

        #region Private Fields

        private bool _initialized;

        #endregion

        #region Private Fields (Observable)

        [ObservableProperty]
        private ApplicationTheme _currentAppTheme;

        [ObservableProperty]
        private bool _confirmBeforeExit;

        [ObservableProperty]
        private int _searchHistoryLimit;

        #endregion

        #region Public Properties

        public string AppVersion { get; private set; } = string.Empty;

        public ApplicationTheme[] AvailableThemes { get; } = [
            ApplicationTheme.Light,
            ApplicationTheme.Dark,
            ApplicationTheme.HighContrast
        ];

        #endregion

        #region Public Constructors

        public AppConfigurationViewModel(
            IApplicationContext applicationContext,
            IAppConfigurationContext appConfigurationContext,
            IMediator mediator,
            IPubSubService pubSubService) : base(pubSubService) {
            _applicationContext = Guard.Against.Null(applicationContext, nameof(applicationContext));
            _appConfigurationContext = Guard.Against.Null(appConfigurationContext, nameof(appConfigurationContext));
            _mediator = Guard.Against.Null(mediator, nameof(mediator));
        }

        #endregion

        #region Event Handlers

        partial void OnCurrentAppThemeChanged(ApplicationTheme oldValue, ApplicationTheme newValue) {
            if (!_initialized) { return; }
            if (oldValue == newValue) { return; }

            ApplicationThemeManager.Apply(newValue);

            _appConfigurationContext.Theme = newValue.ToString();
        }

        partial void OnSearchHistoryLimitChanged(int oldValue, int newValue) {
            if (!_initialized) { return; }
            if (oldValue == newValue) { return; }

            _appConfigurationContext.SearchHistoryLimit = newValue;
        }

        partial void OnConfirmBeforeExitChanged(bool oldValue, bool newValue) {
            if (!_initialized) { return; }
            if (oldValue == newValue) { return; }

            _appConfigurationContext.ConfirmBeforeExit = newValue;
        }

        #endregion

        #region Private Methods

        private void Initialize() {
            if (_initialized) { return; }

            AppVersion = _applicationContext.SemVer;

            if (Enum.TryParse<ApplicationTheme>(_appConfigurationContext.Theme, out var theme)) {
                CurrentAppTheme = theme;
            }

            SearchHistoryLimit = _appConfigurationContext.SearchHistoryLimit;
            ConfirmBeforeExit = _appConfigurationContext.ConfirmBeforeExit;

            _initialized = true;
        }

        #endregion

        #region Private Methods (Commands)

        [RelayCommand]
        private Task OpenApplicationDataDirectoryAsync() {
            using var process = Process.Start(
                fileName: "explorer.exe",
                arguments: _applicationContext.ApplicationDataFolderPath
            );

            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task OpenApplicationLogFileAsync() {
            using var process = Process.Start(
                fileName: "notepad.exe",
                arguments: CoreRoot.Names.LOG_FILE
            );

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task PerformApplicationDatabaseBackupAsync() {
            UIHelper.Instance.ToggleBusyState();

            var response = await _mediator.Send(new PerformDatabaseBackupRequest());

            await PubSubService
                .PublishAsync(new SnackbarNotification {
                    Title = "Backup de Base de Dados",
                    Message = response.Succeeded()
                        ? "Backup realizado com sucesso. Arquivo poderá ser encontrado na pasta de backups dentro do diretório de dados da aplicação."
                        : response.Error,
                    Severity = response.Succeeded()
                        ? Severity.Success
                        : Severity.Error
                });
        }

        #endregion

        #region INavigationAware Members

        public void OnNavigatedFrom() { }

        public void OnNavigatedTo()
            => Initialize();

        #endregion
    }
}
