using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Indexing;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.UpdateContent;
using Nameless.InfoPhoenix.Notification;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

public partial class DocumentDirectoriesPage : INavigableView<DocumentDirectoriesPageViewModel>, INavigationAware, INotificationAware {
    public static readonly string PageName = "Diretórios de Documentos";
    public static readonly string PageToolTip = "Diretórios de Documentos";

    private readonly INotificationService _notificationService;

    private bool _initialized;

    public DocumentDirectoriesPageViewModel ViewModel { get; }

    public DocumentDirectoriesPage(DocumentDirectoriesPageViewModel viewModel,
                                   INotificationService notificationService) {
        ViewModel = Prevent.Argument.Null(viewModel);

        DataContext = ViewModel;

        _notificationService = Prevent.Argument.Null(notificationService);

        InitializeComponent();
        Initialize();
    }

    public void OnNavigatedTo()
        => UpdateViewModelHandler();

    public void OnNavigatedFrom() { }

    public void SubscribeForNotifications() {
        _notificationService.Subscribe<DocumentDirectorySavedNotification>(this, UpdateViewModelHandler);
        _notificationService.Subscribe<DocumentDirectoryIndexedNotification>(this, UpdateViewModelHandler);

        _notificationService.Subscribe<UpdateDocumentDirectoryContentCompletedNotification>(this, UpdateViewModelHandler);
    }

    public void UnsubscribeFromNotifications() {
        _notificationService.Unsubscribe<DocumentDirectorySavedNotification>(this);
        _notificationService.Unsubscribe<DocumentDirectoryIndexedNotification>(this);

        _notificationService.Unsubscribe<UpdateDocumentDirectoryContentCompletedNotification>(this);
    }

    private void Initialize() {
        if (_initialized) { return; }

        SubscribeForNotifications();

        _initialized = true;
    }

    private void UpdateViewModelHandler(object? _ = null, NotificationBase? __ = null)
        => ViewModel.UpdateViewModelCommand.Execute(parameter: null);
}