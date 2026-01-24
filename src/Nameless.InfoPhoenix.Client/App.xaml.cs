using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nameless.InfoPhoenix.Bootstrap.Steps;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Office;
using Nameless.InfoPhoenix.Services;
using Nameless.Infrastructure;
using Nameless.IO.FileSystem;
using Nameless.Lucene;
using Nameless.Mediator;
using Nameless.Validation.FluentValidation;
using Nameless.WPF;
using Nameless.WPF.Behaviors;
using Nameless.WPF.Bootstrap;
using Nameless.WPF.Configuration;
using Nameless.WPF.Dialogs.FileSystem;
using Nameless.WPF.Dialogs.Message;
using Nameless.WPF.GitHub;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Navigation;
using Nameless.WPF.Notifications;
using Nameless.WPF.Snackbar;
using Nameless.WPF.TaskRunner;
using Nameless.WPF.Windows;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Client;

/// <summary>
///     Entry point
/// </summary>
public partial class App {
    private static readonly Assembly[] SupportAssemblies = [
        typeof(ClientAssemblyMarker).Assembly,
        typeof(CoreAssemblyMarker).Assembly,
        typeof(WPFAssemblyMarker).Assembly,
    ];

    private static readonly string[] Args = [
        $"--applicationName={Constants.Application.Name}"
    ];

    private static readonly IHost CurrentHost = HostFactory.Create(Args)
                                                           .ConfigureServices(ConfigureServices)
                                                           .OnStartup(OnHostStartup)
                                                           .OnTearDown(OnHostTearDown)
                                                           .Build();

    // ReSharper disable once AsyncVoidEventHandlerMethod
    protected override async void OnStartup(StartupEventArgs args) {
        await CurrentHost.StartAsync()
                         .SkipContextSync();

        base.OnStartup(args);
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    protected override async void OnExit(ExitEventArgs args) {
        await CurrentHost.StopAsync()
                         .SkipContextSync();

        CurrentHost.Dispose();

        base.OnExit(args);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        services.AddOptions();
        services.ConfigureHttpClientDefaults(builder => {
            builder.ConfigureHttpClient((provider, client) => {
                var applicationContext = provider.GetRequiredService<IApplicationContext>();
                var productHeaderValue = new ProductHeaderValue(applicationContext.ApplicationName, applicationContext.Version);
                var userAgent = new ProductInfoHeaderValue(productHeaderValue);

                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
            });
        });
        services.RegisterAppConfigurationManager();
        services.RegisterAppDbContext(opts => {
            opts.RegisterInterceptor<AuditableEntitySaveChangesInterceptor>();
        });
        services.RegisterApplicationContext(opts => {
            opts.ApplicationName = Constants.Application.Name;
            opts.Version = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);
        });
        services.RegisterBootstrapper(opts => {
            opts.RegisterStep<InitializeDbContextStep>();
            opts.RegisterStep<SeedDatabaseStep>();
        });
        services.RegisterContentDialogService();
        services.RegisterFileSystemDialog();
        services.RegisterMessageDialog();
        services.RegisterDocumentServices(opts => {
            opts.Assemblies = SupportAssemblies;
        });
        services.RegisterFileSystem(opts => opts.AllowOperationOutsideRoot = true);
        services.RegisterGitHubHttpClient(configuration);
        services.RegisterLogging();
        services.RegisterMediator(opts => {
            opts.Assemblies = SupportAssemblies;
            opts.RegisterRequestPipelineBehavior(typeof(ValidateRequestPipelineBehavior<,>))
                .RegisterRequestPipelineBehavior(typeof(PerformanceRequestPipelineBehavior<,>));
        });
        services.RegisterNavigation(opts => {
            opts.Assemblies = SupportAssemblies;
        });
        services.RegisterNotificationService();
        services.RegisterOffice();
        services.RegisterSnackbarService();
        services.RegisterTaskRunner();
        services.RegisterTimeProvider();
        services.RegisterValidation(opts => {
            opts.Assemblies = SupportAssemblies;
        });
        services.RegisterViewModels(opts => {
            opts.Assemblies = SupportAssemblies;
        });
        services.RegisterWindowFactory(opts => {
            opts.Assemblies = SupportAssemblies;
        });
        services.RegisterLucene(opts => opts.SetDirectoryName(
            Path.Combine(Constants.Index.DirectoryName, Constants.Index.Name)
        ));
        services.RegisterZipService();
    }

    // ReSharper disable once AsyncVoidMethod
    private static void OnHostStartup(IServiceProvider provider) {
        var main = provider.GetRequiredService<INavigationWindow>();
        var splashScreen = provider.GetRequiredService<ISplashScreenWindow>();

        splashScreen.Show(WindowStartupLocation.CenterScreen);

        main.ShowWindow();
    }

    // ReSharper disable once AsyncVoidMethod
    private static async void OnHostTearDown(IServiceProvider provider) {
        // Saves the application configuration before exit.
        await provider.GetRequiredService<IAppConfigurationManager>()
                      .SaveChangesAsync(CancellationToken.None)
                      .SkipContextSync();
    }
}
