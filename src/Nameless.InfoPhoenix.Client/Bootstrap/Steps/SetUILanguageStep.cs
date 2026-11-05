using Microsoft.Extensions.Logging;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Bootstrap;
using Nameless.Windows.Bootstrap.Notification;
using Nameless.Windows.Helpers;
using Nameless.Windows.Localization;

namespace Nameless.InfoPhoenix.Client.Bootstrap.Steps;

/// <summary>
/// Applies the persisted UI language before the splash screen closes (plan task 7/A1), so a
/// language chosen on a previous run actually takes effect on the next launch.
/// </summary>
public class SetUILanguageStep : StepBase {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly ILogger<SetUILanguageStep> _logger;
    private readonly ITranslator _translator;

    public SetUILanguageStep(
        IAppConfigurationManager appConfigurationManager,
        ILogger<SetUILanguageStep> logger,
        ITranslator translator) {
        _appConfigurationManager = appConfigurationManager;
        _logger = logger;
        _translator = translator;
    }

    public override Task ExecuteAsync(IProgress<StepProgress> progress, CancellationToken cancellationToken) {
        // Guarded (plan 2.2): an unreadable app.config must not fail the bootstrap step, which
        // would raise a BootstrapException and keep the app from ever starting.
        string? language = null;
        try {
            language = _appConfigurationManager.Language;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to read the stored UI language; keeping the current culture.");
        }

        if (language is not null && CultureResolver.TryGet(language, out var culture)) {
            _translator.SetCulture(culture);
        }

        return Task.CompletedTask;
    }
}
