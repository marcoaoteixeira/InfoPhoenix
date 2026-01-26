using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.InfoPhoenix.Client.Resources;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.UseCases.Application.Backup;
using Nameless.InfoPhoenix.UseCases.Database.Backup;
using Nameless.InfoPhoenix.UseCases.Index.Backup;
using Nameless.Infrastructure;
using Nameless.Mediator;
using Nameless.WPF;
using Nameless.WPF.Configuration;
using Nameless.WPF.Dialogs.Message;
using Nameless.WPF.Helpers;
using Nameless.WPF.Mvvm;
using Nameless.WPF.TaskRunner;
using Nameless.WPF.UseCases.SystemUpdate.Check;
using Nameless.WPF.UseCases.SystemUpdate.Download;
using Nameless.WPF.UseCases.SystemUpdate.Fetch;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class AppConfigurationPageViewModel : ViewModel, INavigationAware {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IApplicationContext _applicationContext;
    private readonly IMediator _mediator;
    private readonly IMessageDialog _messageDialog;
    private readonly ITaskRunner _taskRunner;

    private bool _initialized;

    [ObservableProperty]
    private ComboBoxItem _currentTheme = ComboBoxItemHelper.EmptyComboBoxItem;

    [ObservableProperty]
    private bool _currentConfirmBeforeExit;

    [ObservableProperty]
    private ComboBoxItem _currentSearchHistoryLimit = ComboBoxItemHelper.EmptyComboBoxItem;

    [ObservableProperty]
    private bool _currentEnableDocumentViewer;

    public string AppVersion { get; private set; } = string.Empty;

    public ComboBoxItem[] AvailableThemes { get; } = [
        Theme.Light.ToComboBoxItem(),
        Theme.Dark.ToComboBoxItem(),
        Theme.HighContrast.ToComboBoxItem()
    ];

    public ComboBoxItem[] AvailableSearchHistoryLimit { get; } = [
        SearchHistoryLimit.None.ToComboBoxItem(),
        SearchHistoryLimit.Small.ToComboBoxItem(),
        SearchHistoryLimit.Medium.ToComboBoxItem(),
        SearchHistoryLimit.Large.ToComboBoxItem(),
    ];

    public AppConfigurationPageViewModel(
        IAppConfigurationManager appConfigurationManager,
        IApplicationContext applicationContext,
        IMediator mediator,
        IMessageDialog messageDialog,
        ITaskRunner taskRunner) {
        _appConfigurationManager = appConfigurationManager;
        _applicationContext = applicationContext;
        _mediator = mediator;
        _messageDialog = messageDialog;
        _taskRunner = taskRunner;

        Initialize();
    }

    public Task OnNavigatedToAsync() {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync() {
        return Task.CompletedTask;
    }

    private void Initialize() {
        if (_initialized) { return; }

        AppVersion = _applicationContext.Version;

        var theme = _appConfigurationManager.GetTheme();

        CurrentTheme = theme.GetComboBoxItem(AvailableThemes);
        CurrentConfirmBeforeExit = _appConfigurationManager.GetConfirmBeforeExit();

        var searchHistoryLimit = _appConfigurationManager.GetSearchHistoryLimit();
        CurrentSearchHistoryLimit = searchHistoryLimit.GetComboBoxItem(AvailableSearchHistoryLimit);
        CurrentEnableDocumentViewer = _appConfigurationManager.GetEnableDocumentViewer();

        _initialized = true;
    }

    [RelayCommand]
    private Task PerformSystemUpdateAsync() {
        return _taskRunner.CreateBuilder()
                          .SetName(Strings.AppConfigurationPageViewModel_PerformSystemUpdate_TaskRunnerWindow_Title)
                          .SubscribeFor<CheckForUpdateNotification>()
                          .SubscribeFor<FetchNewVersionInformationNotification>()
                          .SubscribeFor<DownloadUpdateNotification>()
                          .SetDelegate(ExecuteSystemUpdateAsync)
                          .RunAsync();
    }

    [RelayCommand]
    private Task OpenApplicationDataDirectoryAsync() {
        ProcessHelper.OpenDirectory(_applicationContext.DataDirectoryPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenApplicationLogFileAsync() {
        ProcessHelper.OpenTextFile(Constants.Application.LogFileName);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task PerformApplicationBackupAsync() {
        return _taskRunner.CreateBuilder()
                          .SetName(Strings.AppConfigurationPageViewModel_PerformApplicationBackup_TaskRunnerWindow_Title)
                          .SubscribeFor<PerformApplicationBackupNotification>()
                          .SubscribeFor<PerformDatabaseBackupNotification>()
                          .SubscribeFor<PerformDocumentIndexBackupNotification>()
                          .SetDelegate(InnerPerformApplicationBackupAsync)
                          .RunAsync();
    }

    partial void OnCurrentThemeChanged(ComboBoxItem? oldValue, ComboBoxItem newValue) {
        if (!_initialized || oldValue?.Tag == newValue.Tag) { return; }

        var theme = (Theme)newValue.Tag;

        ApplicationThemeManager.Apply(theme.ToApplicationTheme());

        _appConfigurationManager.SetTheme(theme);
    }

    partial void OnCurrentConfirmBeforeExitChanged(bool oldValue, bool newValue) {
        if (!_initialized || oldValue == newValue) { return; }

        _appConfigurationManager.SetConfirmBeforeExit(newValue);
    }

    partial void OnCurrentSearchHistoryLimitChanged(ComboBoxItem? oldValue, ComboBoxItem newValue) {
        if (!_initialized || oldValue?.Tag == newValue.Tag) { return; }

        var searchHistoryLimit = (SearchHistoryLimit)newValue.Tag;

        _appConfigurationManager.SetSearchHistoryLimit(searchHistoryLimit);
    }

    partial void OnCurrentEnableDocumentViewerChanged(bool oldValue, bool newValue) {
        if (!_initialized || oldValue == newValue) { return; }

        _appConfigurationManager.SetEnableDocumentViewer(newValue);
    }

    private async Task InnerPerformApplicationBackupAsync(CancellationToken cancellationToken) {
        _ = await _mediator.ExecuteAsync(new PerformApplicationBackupRequest(), cancellationToken)
                           .SkipContextSync();
    }

    private async Task ExecuteSystemUpdateAsync(CancellationToken cancellationToken) {
        var checkForUpdateResponse = await ExecuteCheckForUpdateAsync(cancellationToken).SkipContextSync();

        if (!checkForUpdateResponse.Success) { return; }

        if (!checkForUpdateResponse.Value.IsNewVersionAvailable) {
            _messageDialog.ShowInformation(
                title: Strings.AppConfigurationPageViewModel_ExecuteSystemUpdateAsync_UpdateUnavailable_MessageBox_Title,
                message: Strings.AppConfigurationPageViewModel_ExecuteSystemUpdateAsync_UpdateUnavailable_MessageBox_Message
            );

            return;
        }

        var result = _messageDialog.ShowQuestion(
            title: Strings.AppConfigurationPageViewModel_ExecuteSystemUpdateAsync_ConfirmDownload_MessageBox_Title,
            message: Strings.AppConfigurationPageViewModel_ExecuteSystemUpdateAsync_ConfirmDownload_MessageBox_Message
        );

        if (result == MessageDialogResult.No) { return; }

        var fetchNewVersionInformationResponse = await ExecuteFetchNewVersionInformationAsync(
            checkForUpdateResponse.Value.ReleaseID,
            checkForUpdateResponse.Value.ApplicationName,
            checkForUpdateResponse.Value.Version,
            cancellationToken
        ).SkipContextSync();

        if (!fetchNewVersionInformationResponse.Success) { return; }

        if (!fetchNewVersionInformationResponse.Value.IsNewVersionAvailable) {
            _messageDialog.ShowAttention(
                title: Strings.AppConfigurationPageViewModel_ExecuteSystemUpdateAsync_AssetNotFound_MessageBox_Title,
                message: Strings.AppConfigurationPageViewModel_ExecuteSystemUpdateAsync_AssetNotFound_MessageBox_Message
            );

            return;
        }

        await ExecuteDownloadUpdateAsync(
            version: checkForUpdateResponse.Value.Version,
            url: fetchNewVersionInformationResponse.Value.Url,
            cancellationToken: cancellationToken
        ).SkipContextSync();
    }

    private Task<CheckForUpdateResponse> ExecuteCheckForUpdateAsync(CancellationToken cancellationToken) {
        return _mediator.ExecuteAsync(new CheckForUpdateRequest(), cancellationToken);
    }

    private Task<FetchNewVersionInformationResponse> ExecuteFetchNewVersionInformationAsync(int releaseID, string applicationName, string version, CancellationToken cancellationToken) {
        var request = new FetchNewVersionInformationRequest(releaseID, applicationName, version);

        return _mediator.ExecuteAsync(request, cancellationToken);
    }

    private Task<DownloadUpdateResponse> ExecuteDownloadUpdateAsync(string version, string url, CancellationToken cancellationToken) {
        return _mediator.ExecuteAsync(new DownloadUpdateRequest(version, url), cancellationToken);
    }
}
