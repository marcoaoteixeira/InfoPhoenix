using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.InfoPhoenix.Client.Views.Windows;
using NLog.Extensions.Logging;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Client {
    public static class ServiceCollectionExtension {
        #region Public Static Methods

        public static IServiceCollection RegisterLogging(this IServiceCollection self)
            => self
                .AddLogging(configure => {
                    configure.ClearProviders();
                    configure.AddNLog();
                });

        public static IServiceCollection RegisterViews(this IServiceCollection self)
            => self
               .AddSingleton<INavigationWindow, MainWindow>()
               .AddSingleton<AppConfigurationPage>()
               .AddSingleton<ListDocumentDirectoryPage>()
               .AddSingleton<SearchPage>()

               .AddTransient<DocumentDirectoryFormPage>()
               .AddTransient<ShowSearchResultWindow>()
               .AddTransient<DocumentViewerWindow>();

        public static IServiceCollection RegisterViewModels(this IServiceCollection self)
            => self
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<AppConfigurationViewModel>()
                .AddSingleton<ListDocumentDirectoryViewModel>()
                .AddSingleton<SearchViewModel>()

                .AddTransient<DocumentDirectoryFormViewModel>()
                .AddTransient<ShowSearchResultWindowViewModel>();

        #endregion
    }
}
