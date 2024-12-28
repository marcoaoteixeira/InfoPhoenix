using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Application.ErrorHandling;
using Nameless.InfoPhoenix.Client.Contracts.Views.Forms;
using Nameless.InfoPhoenix.Client.Contracts.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Forms;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Client.Views.Forms;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Client;

public static class ServiceCollectionExtension {
    public static IServiceCollection RegisterExceptionWarden(this IServiceCollection self)
        => self.AddSingleton<IExceptionWarden, ExceptionWarden>();

    public static IServiceCollection RegisterViewModels(this IServiceCollection self)
        => self.AddSingleton<MainWindowViewModel>()
               .AddSingleton<DocumentDirectoriesPageViewModel>()
               .AddSingleton<SearchPageViewModel>()
               .AddSingleton<AppConfigurationPageViewModel>()

               // ViewModels for forms/dialogs must be transient (open/close multiple times)
               .AddTransient<DocumentDirectoryFormViewModel>()
               .AddTransient<DisplayDocumentSearchResultWindowViewModel>()
               .AddTransient<DocumentViewerWindowViewModel>();

    public static IServiceCollection RegisterViews(this IServiceCollection self)
        => self.AddSingleton<INavigationWindow, MainWindow>()
               .AddSingleton<DocumentDirectoriesPage>()
               .AddSingleton<SearchPage>()
               .AddSingleton<AppConfigurationPage>()

               // Dialogs must be transient (open/close multiple times)
               .AddTransient<IDocumentDirectoryForm, DocumentDirectoryForm>()
               .AddTransient<IDisplayDocumentSearchResultWindow, DisplayDocumentSearchResultWindow>()
               .AddTransient<IDocumentViewerWindow, DocumentViewerWindow>();
}