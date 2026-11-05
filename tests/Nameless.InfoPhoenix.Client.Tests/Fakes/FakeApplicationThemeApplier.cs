using System.Windows;
using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IApplicationThemeApplier"/> fake that just records calls, instead of
/// touching WPF-UI's process-wide static theme state.
/// </summary>
internal sealed class FakeApplicationThemeApplier : IApplicationThemeApplier {
    public Window? AppliedToWindow { get; private set; }

    public Theme? AppliedTheme { get; private set; }

    public List<Theme> AppliedThemes { get; } = [];

    public void Apply(Window window, Theme theme) {
        AppliedToWindow = window;

        Apply(theme);
    }

    public void Apply(Theme theme) {
        AppliedTheme = theme;
        AppliedThemes.Add(theme);
    }
}
