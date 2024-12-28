using System.Windows;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Client.Contracts.Views.Forms;
using Nameless.InfoPhoenix.Client.ViewModels.Forms;
using Nameless.InfoPhoenix.Dialogs;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;
using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Client.Views.Forms;

public partial class DocumentDirectoryForm : IDocumentDirectoryForm,
                                             INotificationAware {
    private readonly INotificationService _notificationService;

    public DocumentDirectoryFormViewModel ViewModel { get; }

    public DocumentDirectoryForm(DocumentDirectoryFormViewModel viewModel,
                                 INotificationService notificationService) {
        ViewModel = Prevent.Argument.Null(viewModel);

        DataContext = ViewModel;

        _notificationService = Prevent.Argument.Null(notificationService);

        SubscribeForNotifications();

        InitializeComponent();
    }

    public void Initialize(Guid documentDirectoryID)
        => ViewModel.InitializeCommand.Execute(documentDirectoryID);

    public void SetTitle(string title)
        => ViewModel.Title = title;

    public void SetOwner(object? owner)
        => Owner = owner as Window ?? System.Windows.Application.Current.MainWindow;

    public DialogResult ShowDialog(StartupLocation startupLocation) {
        WindowStartupLocation = startupLocation.ToWindowStartupLocation();

        ShowDialog();
        
        return Dialogs.DialogResult.Confirm;
    }

    void IWindow.Show(StartupLocation startupLocation) { }

    public void SubscribeForNotifications()
        => _notificationService.Subscribe<DocumentDirectorySavedNotification>(this, NotificationHandler);

    public void UnsubscribeFromNotifications()
        => _notificationService.Unsubscribe<DocumentDirectorySavedNotification>(this);

    private void CloseHandler(object _, RoutedEventArgs __) {
        DialogResult = true;

        UnsubscribeFromNotifications();

        Close();
    }

    private void NotificationHandler(object _, NotificationBase notification) {
        if (notification.Type == NotificationType.Success) {
            CloseHandler(this, new RoutedEventArgs());
        }
    }
}