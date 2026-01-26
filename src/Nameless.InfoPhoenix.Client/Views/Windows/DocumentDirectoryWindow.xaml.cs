using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Notifications;
using Nameless.WPF.Windows;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

[ServiceLifetime(Lifetime = ServiceLifetime.Transient)]
public partial class DocumentDirectoryWindow : IDocumentDirectoryWindow, IHasViewModel<DocumentDirectoryWindowViewModel> {
    private readonly INotificationService _notificationService;

    public DocumentDirectoryWindowViewModel ViewModel { get; }

    public DocumentDirectoryWindow(DocumentDirectoryWindowViewModel viewModel, INotificationService notificationService) {
        _notificationService = notificationService;

        ViewModel = viewModel;
        DataContext = ViewModel;
        Owner = Application.Current.MainWindow;

        InitializeComponent();
    }

    public void SetTitle(string title) {
        ViewModel.Title = title;
    }

    public void SetDocumentDirectory(Guid id) {
        Dispatcher.Invoke(() => ViewModel.UpdateViewModelCommand.Execute(id));
    }

    public void Show(WindowStartupLocation startupLocation) {
        WindowStartupLocation = startupLocation;

        Subscribe();

        ShowDialog();
    }

    private void CloseHandler(object sender, RoutedEventArgs args) {
        DialogResult = true;

        Unsubscribe();

        Close();
    }

    private void Subscribe() {
        _notificationService.Subscribe<CreateNewDocumentDirectoryNotification>(this, NotificationHandler);
        _notificationService.Subscribe<UpdateDocumentDirectoryNotification>(this, NotificationHandler);
    }

    private void Unsubscribe() {
        _notificationService.Unsubscribe<CreateNewDocumentDirectoryNotification>(this);
        _notificationService.Unsubscribe<UpdateDocumentDirectoryNotification>(this);
    }

    private void NotificationHandler(object _, INotification notification) {
        if (notification.Type == NotificationType.Success) {
            CloseHandler(this, new RoutedEventArgs());
        }
    }
}

public interface IDocumentDirectoryWindow : IWindow {
    void SetTitle(string title);
    void SetDocumentDirectory(Guid id);
}
