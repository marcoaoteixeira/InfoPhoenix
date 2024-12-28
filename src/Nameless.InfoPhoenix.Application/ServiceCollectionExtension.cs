using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Application.Dialogs;
using Nameless.InfoPhoenix.Application.Navigation;
using Nameless.InfoPhoenix.Application.Notification;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Dialogs;
using Nameless.InfoPhoenix.Notification;
using Wpf.Ui;
using IWindowFactory = Nameless.InfoPhoenix.Application.Windows.IWindowFactory;

namespace Nameless.InfoPhoenix.Application;

public static class ServiceCollectionExtension {
    public static IServiceCollection RegisterContentDialogService(this IServiceCollection self)
        => self.AddSingleton<IContentDialogService, ContentDialogService>();

    public static IServiceCollection RegisterDialogService(this IServiceCollection self)
        => self.AddSingleton<IDialogService, DialogService>();

    public static IServiceCollection RegisterNavigationService(this IServiceCollection self)
        => self.AddSingleton<INavigationService, NavigationService>();
    
    public static IServiceCollection RegisterPageService(this IServiceCollection self)
        => self.AddSingleton<IPageService, PageService>();
    
    public static IServiceCollection RegisterPubSubService(this IServiceCollection self)
        => self.AddSingleton<INotificationService>(new NotificationService(new WeakReferenceMessenger()));

    public static IServiceCollection RegisterSnackbarService(this IServiceCollection self)
        => self.AddSingleton<ISnackbarService, SnackbarService>();

    public static IServiceCollection RegisterWindowFactory(this IServiceCollection self)
        => self.AddSingleton<IWindowFactory, WindowFactory>();
}