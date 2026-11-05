using System.Globalization;
using System.IO;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.Application;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Features.OpenCurrentLogFile;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;
using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.Mediator;
using Nameless.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Events;
using Nameless.Windows.Helpers;
using Nameless.Windows.Localization;
using Nameless.Windows.Mvvm;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.BackupRecovery;
using Nameless.Windows.Notification.SnackBar;
using Nameless.Windows.TaskRunner;
using Nameless.Windows.UI;
using Wpf.Ui.Abstractions.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

/// <summary>
/// View model backing <c>AppConfigurationPage</c>. Each setting change applies live (theme,
/// culture) and persists immediately through a per-instance save chain (see
/// <see cref="QueueSave"/>) instead of waiting for application shutdown.
/// </summary>
public partial class AppConfigurationPageViewModel : ViewModel, INavigationAware {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IApplicationContext _applicationContext;
    private readonly IApplicationThemeApplier _applicationThemeApplier;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMediator _mediator;
    private readonly ITaskRunner _taskRunner;
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
        IApplicationThemeApplier applicationThemeApplier,
        ITranslator translator,
        IMediator mediator,
        IEventPublisher eventPublisher,
        ITaskRunner taskRunner,
        ILogger<AppConfigurationPageViewModel> logger) {
        _appConfigurationManager = Throws.When.Null(appConfigurationManager);
        _applicationContext = Throws.When.Null(applicationContext);
        _applicationThemeApplier = Throws.When.Null(applicationThemeApplier);
        _eventPublisher = Throws.When.Null(eventPublisher);
        _mediator = Throws.When.Null(mediator);
        _taskRunner = Throws.When.Null(taskRunner);
        _logger = Throws.When.Null(logger);

        T = Throws.When.Null(translator);
    }

    public async Task OnNavigatedToAsync() {
        var recovered = Initialize();

        if (!recovered) { return; }

        // A failing notification must not break navigation (plan 2.3 step 5).
        try {
            await _eventPublisher.PublishFailureAsync<SnackBarNotificationEvent>(
                message: T["AppConfigurationPage_InvalidSettings_Message"],
                title: T["AppConfigurationPage_InvalidSettings_Title"]);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to publish the application configuration recovery notification.");
        }
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

    /// <summary>
    /// Runs the backup through the TaskRunner window (IPX-0008 §3.3/IPX-0009 §3.3). This command
    /// cannot be unit-tested beyond construction: <see cref="ITaskRunner.CreateBuilder"/> returns
    /// a concrete <see cref="TaskRunnerBuilder"/> that needs a real WPF dispatcher, so all backup
    /// logic lives in <see cref="PerformApplicationBackupRequestHandler"/> instead.
    /// </summary>
    [RelayCommand]
    private async Task PerformApplicationBackupAsync() {
        // D9: drain the IPX-0004 save chain first - the chain swallows its own faults, so this
        // never throws.
        await _pendingSave;

        PerformApplicationBackupResult? result = null;

        try {
            var outcome = await _taskRunner.CreateBuilder()
                .SetName(T["AppConfigurationPage_Backup_TaskRunnerWindow_Title"])
                .SubscribeFor<BackupRecoveryProgressNotificationEvent>()
                .SetDelegate(async ct => result = await _mediator.ExecuteAsync(new PerformApplicationBackupRequest(), ct))
                .RunAsync();

            if (outcome is TaskRunnerOutcome.Completed && result is { Succeeded: true }) {
                await _eventPublisher.PublishSuccessAsync<SnackBarNotificationEvent>(
                    message: T["AppConfigurationPage_Backup_Success_Message", [result.BackupFilePath!]]);
            }
            // Failed or Cancelled: already shown in the TaskRunner window, nothing more to do.
        }
        catch (Exception ex) {
            // Unexpected bug path only - anticipated failures do not throw (IPX-0008 §3.2 rule 2).
            _logger.LogError(ex, "Backup failed unexpectedly.");

            await _eventPublisher.PublishFailureAsync<SnackBarNotificationEvent>(
                message: T["AppConfigurationPage_Backup_Failure_Message", [ex.Message]]);
        }
    }

    /// <summary>
    /// Reads and validates the stored settings (plan 2.3). Returns <see langword="true"/> when
    /// recovery fired (any setting was invalid/unreadable, so all three were reset to
    /// defaults), <see langword="false"/> on the happy path (absent counts as valid/default).
    /// Guarded by <see cref="_initialized"/> so it runs once per VM instance (the VM is
    /// transient, so once per navigation to the page).
    /// </summary>
    private bool Initialize() {
        if (_initialized) { return false; }

        AppVersion = _applicationContext.Version;

        Theme theme;
        CultureInfo language;
        bool confirmBeforeExit;
        var issues = new List<string>();

        try {
            theme = ReadTheme(issues);
            language = ReadLanguage(issues);
            confirmBeforeExit = ReadConfirmBeforeExit(issues);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Application configuration is invalid or unreadable; resetting to defaults.");

            Recover();

            return true;
        }

        if (issues.Count > 0) {
            _logger.LogWarning(
                "Application configuration is invalid or unreadable; resetting to defaults. Affected settings: {Issues}",
                string.Join(", ", issues));

            Recover();

            return true;
        }

        CurrentTheme = theme.Find(AvailableThemes);
        CurrentLanguage = language.Find(AvailableLanguages);
        CurrentConfirmBeforeExit = confirmBeforeExit;

        // Set after assigning the properties above, so the On<X>Changed
        // partials (guarded by this flag) never queue a save for initialization.
        _initialized = true;

        return false;
    }

    /// <summary>
    /// Resets all three settings to their defaults (AC2: never only the bad one), in the order
    /// in-memory reset -> live apply -> VM assignment (plan 2.3 step 2b). Live apply and the VM
    /// assignment always run, even when the reset could not be saved (a persistently
    /// unreadable file, AC2 "no stuck/blank state").
    /// </summary>
    private void Recover() {
        try {
            _appConfigurationManager.Theme = Constants.AppConfiguration.Defaults.Theme;
            _appConfigurationManager.Language = Constants.AppConfiguration.Defaults.Language;
            _appConfigurationManager.ConfirmBeforeExit = Constants.AppConfiguration.Defaults.ConfirmBeforeExit;

            QueueSave();
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to reset application configuration to defaults; the reset will not be saved.");
        }

        _applicationThemeApplier.Apply(Constants.AppConfiguration.Defaults.Theme);
        T.SetCulture(Constants.AppConfiguration.Defaults.Language);

        CurrentTheme = Constants.AppConfiguration.Defaults.Theme.Find(AvailableThemes);
        CurrentLanguage = CultureInfo.GetCultureInfo(Constants.AppConfiguration.Defaults.Language).Find(AvailableLanguages);
        CurrentConfirmBeforeExit = Constants.AppConfiguration.Defaults.ConfirmBeforeExit;

        // Set after assigning the properties above (same reason as Initialize's happy path).
        _initialized = true;
    }

    /// <summary>
    /// Absent counts as valid/default (plan 2.3 step 1-2): this is what keeps the happy path
    /// (first run, or an empty <c>{}</c> file) free of false-positive recovery.
    /// </summary>
    private Theme ReadTheme(List<string> issues) {
        var key = Constants.AppConfiguration.Keys.Theme;

        if (!_appConfigurationManager.TryGet<object>(key, out _)) {
            return Constants.AppConfiguration.Defaults.Theme;
        }

        if (!_appConfigurationManager.TryGet<Theme>(key, out var theme)) {
            issues.Add($"{key}:WrongType");

            return Constants.AppConfiguration.Defaults.Theme;
        }

        // ComboBoxItemHelper.EmptyComboBoxItem returns a new instance on every call (no value
        // equality to compare against), so "not found" is detected through its Tag instead -
        // every real entry in AvailableThemes/AvailableLanguages has a non-null Tag.
        if (theme.Find(AvailableThemes).Tag is null) {
            issues.Add($"{key}:Unsupported");

            return Constants.AppConfiguration.Defaults.Theme;
        }

        return theme;
    }

    private CultureInfo ReadLanguage(List<string> issues) {
        var key = Constants.AppConfiguration.Keys.Language;
        var defaultCulture = CultureInfo.GetCultureInfo(Constants.AppConfiguration.Defaults.Language);

        if (!_appConfigurationManager.TryGet<object>(key, out _)) {
            return defaultCulture;
        }

        if (!_appConfigurationManager.TryGet<string>(key, out var isoCode)) {
            issues.Add($"{key}:WrongType");

            return defaultCulture;
        }

        if (!CultureResolver.TryGet(isoCode, out var culture)
            || culture.Find(AvailableLanguages).Tag is null) {
            issues.Add($"{key}:Unsupported");

            return defaultCulture;
        }

        return culture;
    }

    private bool ReadConfirmBeforeExit(List<string> issues) {
        var key = Constants.AppConfiguration.Keys.ConfirmBeforeExit;

        if (!_appConfigurationManager.TryGet<object>(key, out _)) {
            return Constants.AppConfiguration.Defaults.ConfirmBeforeExit;
        }

        if (!_appConfigurationManager.TryGet<bool>(key, out var confirmBeforeExit)) {
            issues.Add($"{key}:WrongType");

            return Constants.AppConfiguration.Defaults.ConfirmBeforeExit;
        }

        return confirmBeforeExit;
    }

    partial void OnCurrentThemeChanged(ComboBoxItem oldValue, ComboBoxItem newValue) {
        // Equals(object, object), not oldValue.Tag == newValue.Tag: the latter is a
        // reference comparison on `object` that only works by accident (plan D6).
        if (!_initialized || Equals(oldValue.Tag, newValue.Tag)) { return; }

        var theme = (Theme)newValue.Tag;

        _applicationThemeApplier.Apply(theme);

        // Only the Set is guarded (plan 2.4): the live change already happened, and the combo
        // keeps the user's choice even when a persistently unreadable file cannot save it.
        try {
            _appConfigurationManager.Theme = theme;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to persist the theme change; the change was applied but not saved.");

            return;
        }

        QueueSave();
    }

    partial void OnCurrentLanguageChanged(ComboBoxItem oldValue, ComboBoxItem newValue) {
        if (!_initialized || Equals(oldValue.Tag, newValue.Tag)) { return; }

        var isoCode = (string)newValue.Tag;

        T.SetCulture(isoCode);

        try {
            _appConfigurationManager.Language = isoCode;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to persist the language change; the change was applied but not saved.");

            return;
        }

        QueueSave();
    }

    partial void OnCurrentConfirmBeforeExitChanged(bool oldValue, bool newValue) {
        if (!_initialized || oldValue == newValue) { return; }

        try {
            _appConfigurationManager.ConfirmBeforeExit = newValue;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to persist the 'confirm before exit' change; the change was applied but not saved.");

            return;
        }

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
        try {
            await previous;
        }
        catch (Exception ex) {
            // A faulted previous save must not poison this (or any later) link in the chain -
            // every subsequent SaveAfterAsync awaits this task, and an unhandled fault here
            // would propagate and silently drop every save queued after it.
            _logger.LogWarning(ex, "A previous application configuration save failed.");
        }

        try {
            await _appConfigurationManager.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logger.LogWarning(ex, "Failed to persist application configuration changes.");
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Unexpected failure while persisting application configuration changes.");
        }
    }

    private static string Quote(string path) {
        return $"\"{path}\"";
    }
}