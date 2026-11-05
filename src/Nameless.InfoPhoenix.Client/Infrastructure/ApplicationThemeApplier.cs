using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nameless.Hosting;
using Nameless.Windows;
using Nameless.Windows.UI;
using Wpf.Ui.Appearance;

namespace Nameless.InfoPhoenix.Client.Infrastructure;

/// <summary>
/// Seam around WPF-UI's static theme application. <see cref="Wpf.Ui.Appearance.SystemThemeWatcher"/>
/// keeps a process-wide, static watch list that broadcasts <see cref="ApplicationThemeManager.Apply"/>
/// to every watched window, including ones a unit test host created on a now-dead thread
/// (plan risk 1) - raising a cross-thread access exception. Production uses
/// <see cref="ApplicationThemeApplier"/>; tests inject a fake that just records the call.
/// </summary>
public interface IApplicationThemeApplier {
    void Apply(Window window, Theme theme);

    /// <summary>
    /// Applies <paramref name="theme"/> without touching any window's watch registration.
    /// <see cref="Theme.System"/> maps to <see cref="ApplicationTheme.Unknown"/> via
    /// <see cref="ThemeExtension.ToApplicationTheme"/>, and
    /// <see cref="ApplicationThemeManager.Apply(ApplicationTheme, Wpf.Ui.Controls.WindowBackdropType, bool)"/>
    /// is a no-op for <see cref="ApplicationTheme.Unknown"/> - so this routes that case through
    /// <see cref="ApplicationThemeManager.ApplySystemTheme(bool)"/> instead.
    /// </summary>
    void Apply(Theme theme);
}

internal sealed class ApplicationThemeApplier : IApplicationThemeApplier {
    public void Apply(Window window, Theme theme) {
        SystemThemeWatcher.Watch(window);
        Apply(theme);
    }

    public void Apply(Theme theme) {
        if (theme == Theme.System) {
            ApplicationThemeManager.ApplySystemTheme();
        }
        else {
            ApplicationThemeManager.Apply(theme.ToApplicationTheme());
        }
    }
}

internal sealed class ApplicationThemeApplierHostFeature : HostFeature {
    public override void Register(IHostApplicationBuilder builder, HostFeatureContext context) {
        builder.Services.AddSingleton<IApplicationThemeApplier, ApplicationThemeApplier>();
    }
}
