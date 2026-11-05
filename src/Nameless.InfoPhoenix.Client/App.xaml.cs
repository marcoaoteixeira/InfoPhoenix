using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.Infrastructure;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.Windows;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Hosting;
using Nameless.Windows.Hosting.Wrappers;
using Nameless.Windows.Localization;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {
    private static WinHost CurrentHost { get; } = WinHostFactory.CreateDefault(
        args: BuildHostArgs(),
        assemblies: [typeof(App).Assembly],
        configureFeatures: ApplicationHostFeature.Configure
    ).Wrap();

    private static string[] BuildHostArgs() {
        var args = new List<string> { $"--applicationName={Constants.Application.Name}" };

        var version = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(version)) {
            args.Add($"--ApplicationContext:Version={version}");
        }

        return [.. args];
    }

    public App() {
        CurrentHost.OnStart += OnCurrentHostStart;
        CurrentHost.OnStop += OnCurrentHostStop;
    }

    protected override async void OnStartup(StartupEventArgs e) {
        await CurrentHost.StartAsync();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e) {
        await CurrentHost.StopAsync();

        CurrentHost.Dispose();

        base.OnExit(e);
    }

    private static Task OnCurrentHostStart(IServiceProvider provider, CancellationToken cancellationToken) {
        TranslatorExtension.SetTranslator(
            provider.GetService<ITranslator>() ?? NullTranslator.Instance
        );

        // Resolve `main` before the splash. If the splash were created or
        // shown first, it would become `Application.MainWindow`, and
        // closing it would shut the app down (default ShutdownMode).
        var main = provider.GetRequiredService<INavigationWindow>();

        provider.GetRequiredService<ISplashScreenWindow>().Show(WindowStartupLocation.CenterScreen);

        main.ShowWindow();

        return Task.CompletedTask;
    }

    // Safety net only (plan 2.4/D1): AppConfigurationPageViewModel already persists every
    // change immediately. This covers in-memory changes made outside the page (the closing
    // handler's "stop asking") and any immediate save that failed. Best-effort: OnExit is
    // async void, so the process may end before this completes.
    private static async Task OnCurrentHostStop(IServiceProvider provider, CancellationToken cancellationToken) {
        await provider.GetRequiredService<IAppConfigurationManager>().SaveChangesAsync(cancellationToken);
    }
}