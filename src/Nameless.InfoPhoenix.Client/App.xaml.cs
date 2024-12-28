using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Bootstrap;
using Nameless.InfoPhoenix.Domains;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Office;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Client;

public partial class App {
    private static readonly Version AppVersion = new(major: 1, minor: 0, build: 0);

    private static readonly Assembly[] SupportAssemblies = [
        typeof(Application.AssemblyMarker).Assembly,
        typeof(AssemblyMarker).Assembly,
        typeof(InfoPhoenix.AssemblyMarker).Assembly,
        typeof(Domains.AssemblyMarker).Assembly,
        typeof(Infrastructure.AssemblyMarker).Assembly,
        typeof(Office.AssemblyMarker).Assembly,
    ];

    private static readonly IHost CurrentHost = HostFactory.Create($"--applicationName={Constants.Common.ApplicationName}")
                                                           .SetConfigureServices(ConfigureServices)
                                                           .Build();

    protected override async void OnStartup(StartupEventArgs e) {
        await CurrentHost.StartAsync();

        // Execute bootstrap steps.
        await CurrentHost.Services
                         .GetRequiredService<IBootstrapper>()
                         .RunAsync();

        // Open the main window.
        CurrentHost.Services
                   .GetRequiredService<INavigationWindow>()
                   .ShowWindow();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e) {
        await CurrentHost.StopAsync();

        CurrentHost.Dispose();

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
        => services.RegisterAppConfigurationManager()
                   .RegisterApplicationContext(AppVersion)
                   .RegisterBootstrapper(SupportAssemblies)
                   .RegisterClock()
                   .RegisterContentDialogService()
                   .RegisterDocumentServices(SupportAssemblies)
                   .RegisterEntityFramework()
                   .RegisterExceptionWarden()
                   .RegisterFileProvider()
                   .RegisterFileSystem()
                   .RegisterFluentValidator(SupportAssemblies)
                   .RegisterLogging()
                   .RegisterLuceneSearch()
                   .RegisterMediator(SupportAssemblies)
                   .RegisterDialogService()
                   .RegisterNavigationService()
                   .RegisterPageService()
                   .RegisterPubSubService()
                   .RegisterSnackbarService()
                   .RegisterTelemetryServices()
                   .RegisterViewModels()
                   .RegisterViews()
                   .RegisterWindowFactory();
}