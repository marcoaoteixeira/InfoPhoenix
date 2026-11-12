using System.Windows;
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

            var footerItems = provider.GetFooterItems().ToList();

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
                new FakeFileSystemDialog(),
                new FakeMessageDialog(),
                new FakeTranslator(),
                new FakeMediator(),
                new FakeEventPublisher(),
                new FakeTaskRunner(),
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

    /// <summary>
    /// AC1/AC2: the Check-for-update button is present but stubbed - disabled and bound to no
    /// command - so interacting with it cannot execute anything (plan D2/D3, F1). The Backup
    /// and Restore cards are no longer stubs (IPX-0009/IPX-0010).
    /// </summary>
    [Theory]
    [InlineData("CheckForUpdateButton")]
    public void StubElement_IsDisabledAndHasNoCommand(string elementName) {
        StaWindowTestHarness.Run(() => {
            var page = CreatePage();

            var element = page.FindName(elementName);

            Assert.NotNull(element);

            var isEnabled = Assert.IsAssignableFrom<UIElement>(element).IsEnabled;
            Assert.False(isEnabled);

            var command = element switch {
                Wpf.Ui.Controls.CardControl card => card.Command,
                Wpf.Ui.Controls.Button button => button.Command,
                _ => throw new InvalidOperationException($"Unexpected element type for '{elementName}': {element.GetType()}.")
            };
            Assert.Null(command);
        });
    }

    /// <summary>
    /// R1: a tooltip set without <c>ToolTipService.ShowOnDisabled="True"</c> never appears on a
    /// disabled element, which would silently defeat the "coming soon" indicator (plan D4).
    /// </summary>
    [Theory]
    [InlineData("CheckForUpdateButton")]
    public void StubElement_HasTooltipThatShowsWhileDisabled(string elementName) {
        StaWindowTestHarness.Run(() => {
            var page = CreatePage();

            var element = Assert.IsAssignableFrom<FrameworkElement>(page.FindName(elementName));

            Assert.NotNull(element.ToolTip);
            Assert.True(ToolTipService.GetShowOnDisabled(element));
        });
    }

    /// <summary>
    /// B13 (IPX-0009 §3.4): the backup card is no longer a stub - it is enabled, and its button
    /// is bound to a real command.
    /// </summary>
    [Fact]
    public void ApplicationBackupCard_IsEnabled_WithButtonBoundToARealCommand() {
        StaWindowTestHarness.Run(() => {
            var page = CreatePage();

            var card = Assert.IsType<Wpf.Ui.Controls.CardControl>(page.FindName("ApplicationBackupCard"));
            var button = Assert.IsType<Wpf.Ui.Controls.Button>(page.FindName("ApplicationBackupButton"));

            Assert.True(card.IsEnabled);
            Assert.NotNull(button.Command);
        });
    }

    /// <summary>
    /// B9 (plan IPX-0010 §6): the restore card is no longer a stub - it is enabled, has no
    /// tooltip, its button is bound to a real command, and the "coming soon" badge is gone.
    /// </summary>
    [Fact]
    public void ApplicationRestoreCard_IsEnabled_WithButtonBoundToARealCommand() {
        StaWindowTestHarness.Run(() => {
            var page = CreatePage();

            var card = Assert.IsType<Wpf.Ui.Controls.CardControl>(page.FindName("ApplicationRestoreCard"));
            var button = Assert.IsType<Wpf.Ui.Controls.Button>(page.FindName("ApplicationRestoreButton"));

            Assert.True(card.IsEnabled);
            Assert.Null(card.ToolTip);
            Assert.NotNull(button.Command);
            Assert.Null(page.FindName("ApplicationRestoreBadge"));
        });
    }

    /// <summary>
    /// AC1: the Version card itself stays enabled (IPX-0004 AC4: the version text must stay
    /// readable) - only its Check-for-update button is disabled (plan D3).
    /// </summary>
    [Fact]
    public void VersionCard_RemainsEnabled_WhileItsCheckForUpdateButtonIsDisabled() {
        StaWindowTestHarness.Run(() => {
            var page = CreatePage();

            var versionCard = Assert.IsType<Wpf.Ui.Controls.CardControl>(page.FindName("AppVersionCard"));
            var checkForUpdateButton = Assert.IsType<Wpf.Ui.Controls.Button>(page.FindName("CheckForUpdateButton"));

            Assert.True(versionCard.IsEnabled);
            Assert.False(checkForUpdateButton.IsEnabled);
        });
    }

    /// <summary>
    /// F2 complement: the "coming soon" badges resolve through the translator to the dedicated
    /// key, not a hardcoded placeholder, so a language switch updates them like every other
    /// localized string on the page.
    /// </summary>
    [Theory]
    [InlineData("CheckForUpdateBadge")]
    public void StubBadge_ContentResolvesThroughTranslator(string badgeName) {
        StaWindowTestHarness.Run(() => {
            var page = CreatePage();

            var badge = Assert.IsType<Wpf.Ui.Controls.Badge>(page.FindName(badgeName));

            Assert.Equal("AppConfigurationPage_ComingSoon_Badge", badge.Content);
        });
    }

    private static AppConfigurationPage CreatePage() {
        // {loc:Translator} bindings resolve through this static translator during
        // construction; the real one is only set up by App's host startup.
        TranslatorExtension.SetTranslator(new FakeTranslator());

        var viewModel = new AppConfigurationPageViewModel(
            new FakeAppConfigurationManager(),
            new FakeApplicationContext(),
            new FakeApplicationThemeApplier(),
            new FakeFileSystemDialog(),
            new FakeMessageDialog(),
            new FakeTranslator(),
            new FakeMediator(),
            new FakeEventPublisher(),
            new FakeTaskRunner(),
            NullLogger<AppConfigurationPageViewModel>.Instance
        );

        var page = new AppConfigurationPage(viewModel);

        // IsEnabled only propagates from a disabled CardControl down to its content once the
        // control's template is applied and its content joins the visual tree - a layout pass
        // forces that, matching what showing the page in the real window does.
        page.Measure(new Size(800, 600));
        page.Arrange(new Rect(0, 0, 800, 600));
        page.UpdateLayout();

        return page;
    }
}