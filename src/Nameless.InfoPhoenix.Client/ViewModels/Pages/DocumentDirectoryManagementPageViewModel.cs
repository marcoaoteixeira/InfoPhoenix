using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.InfoPhoenix.Client.Notifications;
using Nameless.InfoPhoenix.Client.Resources;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.DTOs;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Delete;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.FetchDocuments;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.IndexDocuments;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.List;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;
using Nameless.Mediator;
using Nameless.WPF;
using Nameless.WPF.Dialogs.Message;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Notifications;
using Nameless.WPF.TaskRunner;
using Nameless.WPF.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class DocumentDirectoryManagementPageViewModel : ViewModel, INavigationAware {
    private readonly IMediator _mediator;
    private readonly IMessageDialog _messageDialog;
    private readonly INotificationService _notificationService;
    private readonly ITaskRunner _taskRunner;
    private readonly IWindowFactory _windowFactory;

    private bool _initialized;

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _entries = [];

    public DocumentDirectoryManagementPageViewModel(
        IMediator mediator,
        IMessageDialog messageDialog,
        INotificationService notificationService,
        ITaskRunner taskRunner,
        IWindowFactory windowFactory) {
        _mediator = mediator;
        _messageDialog = messageDialog;
        _notificationService = notificationService;
        _taskRunner = taskRunner;
        _windowFactory = windowFactory;

        Initialize();
    }

    public Task OnNavigatedToAsync() {
        return UpdateViewModelAsync();
    }

    public Task OnNavigatedFromAsync() {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CreateNewDocumentDirectoryAsync() {
        if (_windowFactory.TryCreate<DocumentDirectoryWindow>(out var output)) {
            output.SetDocumentDirectory(Guid.Empty);
            output.Show(WindowStartupLocation.CenterScreen);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DoubleClickDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        Clipboard.SetText(documentDirectory.DirectoryPath);

        var message = $"Caminho para o diretório de documentos '{documentDirectory.Label}' foi copiado para a área de transferência.";
        var notification = DocumentDirectoryManagementNotification.DoubleClick(message);

        return _notificationService.PublishAsync(notification);
    }

    [RelayCommand]
    private Task EditDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        if (_windowFactory.TryCreate<DocumentDirectoryWindow>(out var output)) {
            output.SetDocumentDirectory(documentDirectory.ID);
            output.Show(WindowStartupLocation.CenterScreen);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task FetchDocumentsForDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        return _taskRunner.CreateBuilder()
                          .SetName("Buscar Documentos do Diretório")
                          .SubscribeFor<FetchDocumentsForDocumentDirectoryNotification>()
                          .SetDelegate(token => ExecuteFetchDocumentsForDocumentDirectoryAsync(documentDirectory, token))
                          .RunAsync();
    }

    [RelayCommand]
    private Task IndexDocumentsInDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        return _taskRunner.CreateBuilder()
                          .SetName("Indexar Documentos do Diretório")
                          .SubscribeFor<IndexDocumentsNotification>()
                          .SetDelegate(token => ExecuteIndexDocumentsInDocumentDirectoryAsync(documentDirectory, token))
                          .RunAsync();
    }

    [RelayCommand]
    private Task OpenDocumentDirectoryInExplorerAsync(DocumentDirectoryDto documentDirectory) {
        ProcessHelper.OpenDirectory(documentDirectory.DirectoryPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        var message = string.Format(Strings.DocumentDirectoryManagementPageViewModel_DeleteDocumentDirectoryAsync_MessageBox_Message, documentDirectory.DirectoryPath);
        var result = _messageDialog.Show(message, opts => {
            opts.Title = Strings.DocumentDirectoryManagementPageViewModel_DeleteDocumentDirectoryAsync_MessageBox_Title;
            opts.Buttons = MessageDialogButtons.YesNo;
            opts.Icon = MessageDialogIcon.Warning;
        });

        if (result == MessageDialogResult.No) { return; }

        var request = new DeleteDocumentDirectoryRequest {
            DocumentDirectoryID = documentDirectory.ID
        };

        _ = await _mediator.ExecuteAsync(request, CancellationToken.None)
                           .SkipContextSync();
    }

    private async Task UpdateViewModelAsync() {
        var request = new ListDocumentDirectoriesRequest();
        var response = await _mediator.ExecuteAsync(request, CancellationToken.None)
                                      .SkipContextSync();

        if (!response.Success) { return; }

        Entries = [.. response.Value];
    }

    private void Initialize() {
        if (_initialized) { return; }

        SubscribeForNotification();

        _initialized = true;
    }

    private void SubscribeForNotification() {
        _notificationService.Subscribe<CreateNewDocumentDirectoryNotification>(this, NotificationHandler);
        _notificationService.Subscribe<UpdateDocumentDirectoryNotification>(this, NotificationHandler);
        _notificationService.Subscribe<DeleteDocumentDirectoryNotification>(this, NotificationHandler);
        _notificationService.Subscribe<FetchDocumentsForDocumentDirectoryNotification>(this, NotificationHandler);
        _notificationService.Subscribe<IndexDocumentsNotification>(this, NotificationHandler);
    }

    private void NotificationHandler(object sender, INotification notification) {
        if (notification.Type != NotificationType.Success) {
            return;
        }

        UpdateViewModelAsync().SkipContextSync()
                              .GetAwaiter()
                              .GetResult();
    }

    private async Task ExecuteFetchDocumentsForDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory, CancellationToken cancellationToken) {
        var request = new FetchDocumentsForDocumentDirectoryRequest {
            DocumentDirectoryID = documentDirectory.ID
        };

        _ = await _mediator.ExecuteAsync(request, cancellationToken)
                           .SkipContextSync();
    }

    private async Task ExecuteIndexDocumentsInDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory, CancellationToken cancellationToken) {
        var request = new IndexDocumentsRequest {
            DocumentDirectoryID = documentDirectory.ID
        };

        _ = await _mediator.ExecuteAsync(request, cancellationToken)
                           .SkipContextSync();
    }
}
