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
}

internal sealed class ApplicationThemeApplier : IApplicationThemeApplier {
    public void Apply(Window window, Theme theme) {
        SystemThemeWatcher.Watch(window);
        ApplicationThemeManager.Apply(theme.ToApplicationTheme());
    }
}

internal sealed class ApplicationThemeApplierHostFeature : HostFeature {
    public override void Register(IHostApplicationBuilder builder, HostFeatureContext context) {
        builder.Services.AddSingleton<IApplicationThemeApplier, ApplicationThemeApplier>();
    }
}
