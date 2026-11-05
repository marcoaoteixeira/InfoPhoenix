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
    private readonly ITranslator _translator;

    public SetUILanguageStep(IAppConfigurationManager appConfigurationManager, ITranslator translator) {
        _appConfigurationManager = appConfigurationManager;
        _translator = translator;
    }

    public override Task ExecuteAsync(IProgress<StepProgress> progress, CancellationToken cancellationToken) {
        var language = _appConfigurationManager.Language;

        if (CultureResolver.TryGet(language, out var culture)) {
            _translator.SetCulture(culture);
        }

        return Task.CompletedTask;
    }
}
