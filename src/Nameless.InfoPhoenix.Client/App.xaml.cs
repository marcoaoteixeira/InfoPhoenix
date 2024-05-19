using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nameless.InfoPhoenix.Bootstrapping;
using Nameless.InfoPhoenix.Domain;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Office;
using Nameless.InfoPhoenix.UI.Extensions;
using Wpf.Ui;
using ClientRoot = Nameless.InfoPhoenix.Client.Root;
using CoreRoot = Nameless.InfoPhoenix.Root;
using DomainRoot = Nameless.InfoPhoenix.Domain.Root;
using UIRoot = Nameless.InfoPhoenix.UI.Root;

namespace Nameless.InfoPhoenix.Client {
    public partial class App {
        #region Private Static Read-Only Fields

        private static readonly Assembly[] SupportAssemblies = [
            typeof(ClientRoot).Assembly,
            typeof(CoreRoot).Assembly,
            typeof(DomainRoot).Assembly,
            typeof(UIRoot).Assembly,
        ];

        #endregion

        #region Private Read-Only Fields

        private readonly IHost _host = HostFactory
                                       .Create($"--applicationName={CoreRoot.Names.APPLICATION}")
                                       .SetConfigureServices(ConfigureServices)
                                       .Build();

        #endregion

        #region Protected Override Methods

        protected override async void OnStartup(StartupEventArgs e) {
            _host.Start();

            // Execute bootstrap steps.
            await _host.Services
                 .GetRequiredService<IBootstrap>()
                 .RunAsync();

            // Open the main window.
            _host.Services
                 .GetRequiredService<INavigationWindow>()
                 .ShowWindow();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e) {
            _host.Dispose();

            base.OnExit(e);
        }

        #endregion

        #region Private Static Methods

        private static void ConfigureServices(IServiceCollection services)
            => services.RegisterAppConfigurationContext()
                       .RegisterAppDbContext()
                       .RegisterApplicationContext(
                           useAppDataSpecialFolder: true,
                           appVersion: typeof(App).Assembly.GetName()
                                                  .Version ?? new Version()
                       )
                       .RegisterBootstrap(SupportAssemblies)
                       .RegisterFileProvider()
                       .RegisterLogging()
                       .RegisterLucene()
                       .RegisterMediatR(SupportAssemblies)
                       .RegisterMessageBoxService()
                       .RegisterNavigationService()
                       .RegisterPageService()
                       .RegisterPerformanceWatcher()
                       .RegisterPubSubService()
                       .RegisterSnackbarService()
                       .RegisterOfficeSuite()
                       .RegisterViews()
                       .RegisterViewModels()
                       .RegisterWindowService();

        #endregion
    }
}
