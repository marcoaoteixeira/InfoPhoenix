using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.Application;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Features.OpenCurrentLogFile;
using Nameless.Mediator;
using Nameless.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Helpers;
using Nameless.Windows.Localization;
using Nameless.Windows.Mvvm;
using Nameless.Windows.UI;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

/// <summary>
/// View model backing <c>AppConfigurationPage</c>. Each setting change applies live (theme,
/// culture) and persists immediately through a per-instance save chain (see
/// <see cref="QueueSave"/>) instead of waiting for application shutdown.
/// </summary>
public partial class AppConfigurationPageViewModel : ViewModel, INavigationAware {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IApplicationContext _applicationContext;
    private readonly IMediator _mediator;
    private readonly ILogger<AppConfigurationPageViewModel> _logger;

    private bool _initialized;

    // Chains every save after the previous one so the non-thread-safe
    // AppConfigurationManager dictionary is never serialized concurrently
    // (plan 2.4). Never awaited with ConfigureAwait(false)/SkipContextSync:
    // SaveChangesAsync serializes synchronously at its start, and that must
    // happen on the same (UI) thread that calls Set.
    private Task _pendingSave = Task.CompletedTask;

    private ITranslator T { get; }

    [ObservableProperty]
    public partial ComboBoxItem CurrentTheme { get; set; } = ComboBoxItemHelper.EmptyComboBoxItem;

    [ObservableProperty]
    public partial ComboBoxItem CurrentLanguage { get; set; } = ComboBoxItemHelper.EmptyComboBoxItem;

    [ObservableProperty]
    public partial bool CurrentConfirmBeforeExit { get; set; }

    public string AppVersion { get; private set; } = string.Empty;

    public ComboBoxItem[] AvailableThemes { get; } = [
        Theme.System.ToComboBoxItem(),
        Theme.Light.ToComboBoxItem(),
        Theme.Dark.ToComboBoxItem(),
        Theme.HighContrast.ToComboBoxItem(),
    ];

    public ComboBoxItem[] AvailableLanguages { get; } = [
        ComboBoxItemHelper.Create("pt-BR", "Português (Brasil)"),
        ComboBoxItemHelper.Create("en-GB", "English (United Kingdom)"),
    ];

    public AppConfigurationPageViewModel(
        IAppConfigurationManager appConfigurationManager,
        IApplicationContext applicationContext,
        ITranslator translator,
        IMediator mediator,
        ILogger<AppConfigurationPageViewModel> logger) {
        _appConfigurationManager = Throws.When.Null(appConfigurationManager);
        _applicationContext = Throws.When.Null(applicationContext);
        _mediator = Throws.When.Null(mediator);
        _logger = Throws.When.Null(logger);

        T = Throws.When.Null(translator);
    }

    public Task OnNavigatedToAsync() {
        Initialize();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync() {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenApplicationDataDirectoryAsync() {
        ProcessHelper.OpenDirectory(Quote(_applicationContext.FileManager.Root));

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenApplicationLogFileAsync(CancellationToken cancellationToken) {
        await _mediator.ExecuteAsync(new OpenCurrentLogFileRequest(), cancellationToken);
    }

    private void Initialize() {
        if (_initialized) { return; }

        AppVersion = _applicationContext.Version;

        var theme = _appConfigurationManager.Theme;
        var culture = CultureResolver.TryGet(_appConfigurationManager.Language, out var resolved)
            ? resolved
            : CultureInfo.CurrentUICulture;

        CurrentTheme = theme.Find(AvailableThemes);
        CurrentLanguage = culture.Find(AvailableLanguages);
        CurrentConfirmBeforeExit = _appConfigurationManager.ConfirmBeforeExit;

        // Set after assigning the properties above, so the On<X>Changed
        // partials (guarded by this flag) never queue a save for initialization.
        _initialized = true;
    }

    partial void OnCurrentThemeChanged(ComboBoxItem oldValue, ComboBoxItem newValue) {
        // Equals(object, object), not oldValue.Tag == newValue.Tag: the latter is a
        // reference comparison on `object` that only works by accident (plan D6).
        if (!_initialized || Equals(oldValue.Tag, newValue.Tag)) { return; }

        var theme = (Theme)newValue.Tag;

        ApplicationThemeManager.Apply(theme.ToApplicationTheme());

        _appConfigurationManager.Theme = theme;

        QueueSave();
    }

    partial void OnCurrentLanguageChanged(ComboBoxItem oldValue, ComboBoxItem newValue) {
        if (!_initialized || Equals(oldValue.Tag, newValue.Tag)) { return; }

        var isoCode = (string)newValue.Tag;

        T.SetCulture(isoCode);

        _appConfigurationManager.Language = isoCode;

        QueueSave();
    }

    partial void OnCurrentConfirmBeforeExitChanged(bool oldValue, bool newValue) {
        if (!_initialized || oldValue == newValue) { return; }

        _appConfigurationManager.ConfirmBeforeExit = newValue;

        QueueSave();
    }

    /// <summary>
    /// Chains a save after whatever save is currently pending. Every change still triggers
    /// its own call (no debounce/coalescing, plan 2.4) - this only prevents two saves from
    /// running concurrently against the manager's non-thread-safe dictionary.
    /// </summary>
    private void QueueSave() {
        _pendingSave = SaveAfterAsync(_pendingSave);
    }

    private async Task SaveAfterAsync(Task previous) {
        await previous;

        try {
            await _appConfigurationManager.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to persist application configuration changes.");
        }
    }

    private static string Quote(string path) {
        return $"\"{path}\"";
    }
}
