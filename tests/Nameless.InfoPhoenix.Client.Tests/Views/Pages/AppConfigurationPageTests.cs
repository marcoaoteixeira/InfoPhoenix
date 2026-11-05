using System.Windows.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.Windows.Localization;
using Nameless.Windows.Navigation;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Pages;

/// <summary>
/// AC1: the page shows up in the footer navigation menu, discoverable the same way every other
/// <c>[NavigationViewItem]</c> page is (an assembly scan), independent of bootstrap/splash
/// state - the page and its view model have no dependency on <c>IBootstrapper</c> or any splash
/// type, which this test's constructor list (and a glance at both classes) confirms.
/// </summary>
public class AppConfigurationPageTests {
    [Fact]
    public void AutoDiscoverableNavigationViewItemProvider_DiscoversExactlyOneFooterItemForThePage() {
        StaWindowTestHarness.Run(() => {
            var provider = new AutoDiscoverableNavigationViewItemProvider([typeof(App).Assembly]);

            var footerItems = provider.GetItems(Placement.Bottom).ToList();

            var configurationItems = footerItems.Where(item => item.TargetPageType == typeof(AppConfigurationPage)).ToList();

            Assert.Single(configurationItems);
        });
    }

    [Fact]
    public void AppVersionTextBlock_IsATextBlockNotAnEditableControl() {
        StaWindowTestHarness.Run(() => {
            // {loc:Translator} bindings resolve through this static translator during
            // construction; the real one is only set up by App's host startup.
            TranslatorExtension.SetTranslator(new FakeTranslator());

            var viewModel = new AppConfigurationPageViewModel(
                new FakeAppConfigurationManager(),
                new FakeApplicationContext(),
                new FakeApplicationThemeApplier(),
                new FakeTranslator(),
                new FakeMediator(),
                NullLogger<AppConfigurationPageViewModel>.Instance
            );

            var page = new AppConfigurationPage(viewModel);

            var versionElement = page.FindName("AppVersionTextBlock");

            // AC4 ("not editable"): a TextBlock, never a TextBox. The displayed value itself
            // (AppVersion == IApplicationContext.Version) is covered by
            // AppConfigurationPageViewModelTests.AppVersion_AfterNavigation_MatchesApplicationContextVersion
            // - a OneTime ElementName binding only activates once attached to a loaded visual
            // tree, which a component test would need a real PresentationSource for.
            Assert.IsType<TextBlock>(versionElement);
            Assert.IsNotType<TextBox>(versionElement);
        });
    }
}
