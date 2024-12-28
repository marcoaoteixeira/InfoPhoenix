using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.Application;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Domains.UseCases.Database.PerformBackup;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class AppConfigurationPageViewModel : ViewModelBase, INavigationAware {
    private readonly IAppConfigurationManager _appConfigurationContext;
    private readonly IApplicationContext _applicationContext;
    private readonly IMediator _mediator;

    private bool _initialized;

    [ObservableProperty]
    private ComboBoxItem _currentTheme = ComboBoxItemHelper.EmptyComboBoxItem;

    [ObservableProperty]
    private ComboBoxItem _currentSearchHistoryLimit = ComboBoxItemHelper.EmptyComboBoxItem;

    [ObservableProperty]
    private bool _currentConfirmBeforeExit;

    [ObservableProperty]
    private bool _currentEnableDocumentViewer;

    public string AppVersion { get; private set; } = string.Empty;

    public ComboBoxItem[] AvailableThemes { get; } = [
        Theme.Light.ToComboBoxItem(),
        Theme.Dark.ToComboBoxItem(),
        Theme.HighContrast.ToComboBoxItem()
    ];

    public ComboBoxItem[] AvailableSearchHistoryLimit { get; } = [
        SearchHistoryLimit.Small.ToComboBoxItem(),
        SearchHistoryLimit.Medium.ToComboBoxItem(),
        SearchHistoryLimit.Large.ToComboBoxItem(),
    ];

    public AppConfigurationPageViewModel(IAppConfigurationManager appConfigurationContext,
                                         IApplicationContext applicationContext,
                                         IMediator mediator) {
        _appConfigurationContext = Prevent.Argument.Null(appConfigurationContext);
        _applicationContext = Prevent.Argument.Null(applicationContext);
        _mediator = Prevent.Argument.Null(mediator);
    }

    public void OnNavigatedTo()
        => Initialize();

    public void OnNavigatedFrom()
        => _appConfigurationContext.CommitChanges();

    private void Initialize() {
        if (_initialized) { return; }

        AppVersion = _applicationContext.Version;

        _appConfigurationContext.Initialize();

        var theme = _appConfigurationContext.GetTheme();
        var searchHistoryLimit = _appConfigurationContext.GetSearchHistoryLimit();

        CurrentTheme = theme.GetComboBoxItemFromAvailable(AvailableThemes);
        CurrentSearchHistoryLimit = searchHistoryLimit.GetComboBoxItemFromAvailable(AvailableSearchHistoryLimit);
        CurrentConfirmBeforeExit = _appConfigurationContext.GetConfirmBeforeExit();
        CurrentEnableDocumentViewer = _appConfigurationContext.GetEnableDocumentViewer();

        _initialized = true;
    }

    [RelayCommand]
    private async Task PerformApplicationDatabaseBackupAsync()
        => await _mediator.Send(new PerformDatabaseBackupRequest());

    [RelayCommand]
    private Task OpenApplicationDataDirectoryAsync() {
        ProcessHelper.OpenDirectory(_applicationContext.AppDataFolderPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenApplicationLogFileAsync() {
        ProcessHelper.OpenTextFile(Constants.Files.LogFileName);

        return Task.CompletedTask;
    }

    partial void OnCurrentThemeChanged(ComboBoxItem? oldValue, ComboBoxItem newValue) {
        if (!_initialized || oldValue?.Tag == newValue.Tag) { return; }

        var theme = (Theme)newValue.Tag;

        ApplicationThemeManager.Apply(theme.ToApplicationTheme());

        _appConfigurationContext.SetTheme(theme);
    }

    partial void OnCurrentSearchHistoryLimitChanged(ComboBoxItem? oldValue, ComboBoxItem newValue) {
        if (!_initialized || oldValue?.Tag == newValue.Tag) { return; }

        var searchHistoryLimit = (SearchHistoryLimit)newValue.Tag;

        _appConfigurationContext.SetSearchHistoryLimit(searchHistoryLimit);
    }

    partial void OnCurrentConfirmBeforeExitChanged(bool oldValue, bool newValue) {
        if (!_initialized || oldValue == newValue) { return; }

        _appConfigurationContext.SetConfirmBeforeExit(newValue);
    }

    partial void OnCurrentEnableDocumentViewerChanged(bool oldValue, bool newValue) {
        if (!_initialized || oldValue == newValue) { return; }

        _appConfigurationContext.SetEnableDocumentViewer(newValue);
    }
}