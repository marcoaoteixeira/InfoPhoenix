using System.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.Events;
using Nameless.Windows.Notification;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class DocumentDirectoryWindow : IDocumentDirectoryWindow {
    private readonly IEventPublisher _eventPublisher;

    public DocumentDirectoryWindowViewModel ViewModel { get; }

    public DocumentDirectoryWindow(DocumentDirectoryWindowViewModel viewModel, IEventPublisher eventPublisher) {
        _eventPublisher = eventPublisher;

        ViewModel = viewModel;
        DataContext = ViewModel;
        Owner = System.Windows.Application.Current.MainWindow;

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
    }

    private void Unsubscribe() {
    }

    private void NotificationHandler(object _, NotificationEvent evt) {
        if (evt.Type == NotificationType.Success) {
            CloseHandler(this, new RoutedEventArgs());
        }
    }
}

public interface IDocumentDirectoryWindow : IWindow {
    void SetTitle(string title);
    void SetDocumentDirectory(Guid id);
}
