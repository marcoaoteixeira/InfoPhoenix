using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.Windows.UI;
using Wpf.Ui.Appearance;

namespace Nameless.InfoPhoenix.Client.Tests.Infrastructure;

/// <summary>
/// Review round 1, finding 1: <see cref="Theme.System"/> is never applied, live or at startup,
/// because <c>ToApplicationTheme()</c> maps it to <see cref="ApplicationTheme.Unknown"/>, for
/// which the regular <see cref="ApplicationThemeManager.Apply(ApplicationTheme, Wpf.Ui.Controls.WindowBackdropType, bool)"/>
/// is a no-op. These tests exercise the real WPF-UI static theme manager (not a fake) to prove
/// <see cref="ApplicationThemeApplier"/> actually routes <see cref="Theme.System"/> through
/// <see cref="ApplicationThemeManager.ApplySystemTheme(bool)"/> instead.
/// </summary>
[Collection(WpfUiThemeTestCollection.Name)]
public class ApplicationThemeApplierTests {
    [Fact]
    public void Apply_WithSystemTheme_RoutesThroughApplySystemTheme_NotTheNoOpRegularApplyPath() {
        StaWindowTestHarness.Run(() => {
            // Seed a baseline that deliberately disagrees with the real system theme: the
            // no-op regular Apply(Unknown) path would leave it in place, while
            // ApplySystemTheme() must move it away.
            var systemTheme = ApplicationThemeManager.GetSystemTheme();
            var wrongBaseline = systemTheme == SystemTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;

            ApplicationThemeManager.Apply(wrongBaseline);
            Assert.Equal(wrongBaseline, ApplicationThemeManager.GetAppTheme());

            var applier = new ApplicationThemeApplier();
            applier.Apply(Theme.System);

            Assert.NotEqual(wrongBaseline, ApplicationThemeManager.GetAppTheme());
        });
    }

    [Theory]
    [InlineData(Theme.Light, ApplicationTheme.Light)]
    [InlineData(Theme.Dark, ApplicationTheme.Dark)]
    public void Apply_WithAnExplicitTheme_StillUsesTheRegularApplyPath(Theme theme, ApplicationTheme expected) {
        StaWindowTestHarness.Run(() => {
            var opposite = expected == ApplicationTheme.Light ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(opposite);

            var applier = new ApplicationThemeApplier();
            applier.Apply(theme);

            Assert.Equal(expected, ApplicationThemeManager.GetAppTheme());
        });
    }
}
