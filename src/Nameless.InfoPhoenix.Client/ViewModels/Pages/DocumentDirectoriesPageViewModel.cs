using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Client.Contracts.Views.Forms;
using Nameless.InfoPhoenix.Client.Notifications;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.Dialogs;
using Nameless.InfoPhoenix.Domains.Dtos;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Indexing;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.List;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.UpdateContent;
using Nameless.InfoPhoenix.Notification;
using Nameless.InfoPhoenix.Telemetry;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class DocumentDirectoriesPageViewModel : ViewModelBase {
    private readonly IDialogService _dialogService;
    private readonly IFileSystem _fileSystem;
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly IPerformanceReporter _performanceReporter;
    private readonly IWindowFactory _windowFactory;

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _documentDirectories = [];

    public DocumentDirectoriesPageViewModel(IDialogService dialogService,
                                            IFileSystem fileSystem,
                                            IMediator mediator,
                                            INotificationService notificationService,
                                            IPerformanceReporter performanceReporter,
                                            IWindowFactory windowFactory) {
        _dialogService = Prevent.Argument.Null(dialogService);
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _mediator = Prevent.Argument.Null(mediator);
        _notificationService = Prevent.Argument.Null(notificationService);
        _performanceReporter = Prevent.Argument.Null(performanceReporter);
        _windowFactory = Prevent.Argument.Null(windowFactory);
    }

    [RelayCommand]
    private async Task UpdateViewModelAsync() {
        const string tag = $"{nameof(DocumentDirectoriesPageViewModel)}.{nameof(UpdateViewModelAsync)}";

        using (_performanceReporter.ExecutionTime(tag)) {
            var result = await _mediator.Send(new ListDocumentDirectoriesRequest());

            DocumentDirectories = [.. result];
        }
    }

    [RelayCommand]
    private Task CreateNewDocumentDirectoryAsync() {
        if (_windowFactory.TryCreate<IDocumentDirectoryForm>(owner: null, out var dialog)) {
            dialog.SetTitle("Criar novo Diretório de Documentos");
            dialog.Initialize(Guid.Empty);
            dialog.ShowDialog(StartupLocation.CenterOwner);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        var result = _dialogService.ShowQuestion("Remover Diretório de Documentos",
                                                 $"Deseja realmente remover o diretório de documentos '{documentDirectory.Label}'?",
                                                 DialogButtons.YesNo);

        if (result == DialogResult.Yes) {

        }

        return Task.CompletedTask;
    }

    [RelayCommand] // DoubleClick
    private Task CopyDocumentDirectoryPathClipboardAsync(DocumentDirectoryDto documentDirectory) {
        Clipboard.SetText(documentDirectory.DirectoryPath);

        return _notificationService.PublishAsync(new DocumentDirectoryPathCopiedToClipboardNotification {
            Title = "Área de Transferência",
            Message = $"Caminho para o diretório de documentos '{documentDirectory.Label}' foi copiado para a área de transferência.",
            Type = NotificationType.Information
        });
    }

    [RelayCommand]
    private Task EditDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        if (_windowFactory.TryCreate<IDocumentDirectoryForm>(owner: null, out var dialog)) {
            dialog.SetTitle("Editar Diretório de Documentos");
            dialog.Initialize(documentDirectory.ID);
            dialog.ShowDialog(StartupLocation.CenterOwner);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task IndexDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        ProgressRunnerHelper.Run($"Indexação em '{documentDirectory.Label}'", (progress, cancellationToken) => {
            var request = new IndexDocumentDirectoryRequest {
                DocumentDirectoryID = documentDirectory.ID,
                Reporter = progress
            };

            _mediator.Send(request, cancellationToken);

            return Task.FromResult(ExecutionResult.Empty);
        });

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        if (!_fileSystem.Directory.Exists(documentDirectory.DirectoryPath)) {
            return _notificationService.PublishAsync(new DocumentDirectoryMissingNotification {
                Title = "Diretório Não Localizado",
                Message = $"O diretório de documentos '{documentDirectory.Label}' não pode ser localizado no disco.",
                Type = NotificationType.Warning
            });
        }

        ProcessHelper.OpenDirectory(documentDirectory.DirectoryPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task UpdateDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        ProgressRunnerHelper.Run($"Atualização do Conteúdo de '{documentDirectory.Label}'", (progress, cancellationToken) => {
            var request = new UpdateDocumentDirectoryContentRequest {
                DocumentDirectoryID = documentDirectory.ID,
                Reporter = progress
            };

            _mediator.Send(request, cancellationToken);

            return Task.FromResult(ExecutionResult.Empty);
        });

        return Task.CompletedTask;
    }
}