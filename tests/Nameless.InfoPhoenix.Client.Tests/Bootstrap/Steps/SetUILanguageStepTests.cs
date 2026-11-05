using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Bootstrap.Steps;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Bootstrap.Notification;

namespace Nameless.InfoPhoenix.Client.Tests.Bootstrap.Steps;

/// <summary>
/// B14 (plan task 7/A1): the persisted UI language is applied during bootstrap, before the
/// splash screen closes, so a language chosen on a previous run takes effect on the next one.
/// </summary>
public class SetUILanguageStepTests {
    [Fact]
    public async Task ExecuteAsync_AppliesThePersistedLanguage_ToTheTranslator() {
        var manager = new FakeAppConfigurationManager();
        manager.Language = "en-GB";
        var translator = new FakeTranslator();
        var step = CreateStep(manager, translator);

        await step.ExecuteAsync(new Progress<StepProgress>(_ => { }), CancellationToken.None);

        Assert.Equal(["en-GB"], translator.SetCultureCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnresolvableLanguage_DoesNotThrowAndLeavesTheTranslatorUntouched() {
        var manager = new FakeAppConfigurationManager();
        manager.Language = "not-a-real-culture";
        var translator = new FakeTranslator();
        var step = CreateStep(manager, translator);

        var error = await Record.ExceptionAsync(
            () => step.ExecuteAsync(new Progress<StepProgress>(_ => { }), CancellationToken.None));

        Assert.Null(error);
        Assert.Empty(translator.SetCultureCalls);
    }

    /// <summary>
    /// B13 (plan 2.2): an unreadable manager must not fail the bootstrap step - it would raise
    /// a BootstrapException and keep the app from ever starting.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithAThrowingManager_CompletesWithoutSettingCulture() {
        var manager = new FakeAppConfigurationManager { ThrowOnTryGet = new IOException() };
        var translator = new FakeTranslator();
        var step = CreateStep(manager, translator);

        var error = await Record.ExceptionAsync(
            () => step.ExecuteAsync(new Progress<StepProgress>(_ => { }), CancellationToken.None));

        Assert.Null(error);
        Assert.Empty(translator.SetCultureCalls);
    }

    private static SetUILanguageStep CreateStep(FakeAppConfigurationManager manager, FakeTranslator translator) {
        return new SetUILanguageStep(manager, NullLogger<SetUILanguageStep>.Instance, translator);
    }
}
