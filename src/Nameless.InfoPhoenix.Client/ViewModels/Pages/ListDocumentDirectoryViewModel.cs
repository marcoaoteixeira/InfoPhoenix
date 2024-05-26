using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Client.Views.Pages;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Objects;
using Nameless.InfoPhoenix.UI;
using Nameless.InfoPhoenix.UI.Helpers;
using Nameless.InfoPhoenix.UI.MessageBox;
using Wpf.Ui;
using MessageBoxButton = Nameless.InfoPhoenix.UI.MessageBox.MessageBoxButton;
using MessageBoxResult = Nameless.InfoPhoenix.UI.MessageBox.MessageBoxResult;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages {
    public partial class ListDocumentDirectoryViewModel : ViewModelBase {
        #region Private Read-Only Fields

        private readonly IMediator _mediator;
        private readonly IMessageBoxService _messageBoxService;
        private readonly INavigationService _navigationService;
        private readonly IPerformanceReporter _performanceWatcher;

        #endregion

        #region Private Fields (Observable)

        [ObservableProperty]
        private ObservableCollection<DocumentDirectoryDto> _documentDirectoryCollection = [];

        [ObservableProperty]
        private ObservableCollection<string> _updates = [];

        #endregion

        #region Public Constructors

        public ListDocumentDirectoryViewModel(
            IMediator mediator,
            IMessageBoxService messageBoxService,
            INavigationService navigationService,
            IPerformanceReporter performanceWatcher,
            IPubSubService pubSubService) : base(pubSubService) {
            _mediator = Guard.Against.Null(mediator, nameof(mediator));
            _messageBoxService = Guard.Against.Null(messageBoxService, nameof(messageBoxService));
            _navigationService = Guard.Against.Null(navigationService, nameof(navigationService));
            _performanceWatcher = Guard.Against.Null(performanceWatcher, nameof(performanceWatcher));
        }

        #endregion

        #region Private Methods

        private Task ShowSnackbarAsync(string message, string? title = null, Severity severity = Severity.None)
            => PubSubService.PublishAsync(new SnackbarNotification {
                Title = title,
                Message = message,
                Severity = severity
            });

        private async Task<bool> CollectDocumentsInDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
            var response = await _mediator.Send(new CollectDocumentsFromDocumentDirectoryRequest {
                DocumentDirectoryID = documentDirectory.ID
            });

            await ShowSnackbarAsync(title: "Fase: Coletar Documentos",
                               message: response.Succeeded()
                                   ? "Coleta de Documentos realizada com sucesso. Indexando..."
                                   : response.Error,
                               severity: response.Succeeded()
                                   ? Severity.Success
                                   : Severity.Error);

            return response.Succeeded();
        }

        private async Task IndexDocumentsInDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
            var response = await _mediator.Send(new IndexDocumentDirectoryRequest {
                DocumentDirectoryID = documentDirectory.ID
            });

            await ShowSnackbarAsync(title: "Fase: Indexar Documentos",
                               message: response.Succeeded()
                                   ? "Indexação de Documentos realizada com sucesso. Finalizado!"
                                   : response.Error,
                               severity: response.Succeeded()
                                   ? Severity.Success
                                   : Severity.Error);
        }

        #endregion

        #region Private Methods (Commands)

        [RelayCommand]
        private async Task UpdateViewModelAsync() {
            UIHelper.Instance.ToggleBusyState();

            var procedure = $"{nameof(ListDocumentDirectoryViewModel)}.{nameof(UpdateViewModelAsync)}";
            using (_performanceWatcher.ReportExecutionTime(procedure)) {
                var response = await _mediator
                    .Send(new ListDocumentDirectoriesRequest());

                DocumentDirectoryCollection = [.. response.Value];
            }
        }

        [RelayCommand]
        private Task CopyDirectoryPathClipboardAsync(DocumentDirectoryDto documentDirectory) {
            Clipboard.SetText(documentDirectory.DirectoryPath);

            return ShowSnackbarAsync(
                title: "Área de Transferência",
                message: "Caminho do Diretório de Documentos copiado para a área de transferência.",
                severity: Severity.Information
            );
        }

        [RelayCommand]
        private Task EditDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
            _navigationService
                .Navigate(pageType: typeof(DocumentDirectoryFormPage),
                          dataContext: documentDirectory.ID);

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task IndexDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
            var procedure = $"{nameof(ListDocumentDirectoryViewModel)}.{nameof(IndexDocumentDirectoryAsync)}";
            using (_performanceWatcher.ReportExecutionTime(procedure)) {
                var succeeded = await CollectDocumentsInDocumentDirectoryAsync(documentDirectory);
                if (!succeeded) { return; }

                await IndexDocumentsInDocumentDirectoryAsync(documentDirectory);
                await UpdateViewModelAsync();
            }
        }

        [RelayCommand]
        private Task OpenDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
            if (documentDirectory.Missing) {
                return ShowSnackbarAsync(
                    title: "Diretório não encontrado",
                    message: "Não foi possível abrir o diretório pois não foi localizado no disco. Certifique-se que o mesmo ainda exista.",
                    severity: Severity.Warning
                );
            }

            using var process = Process.Start(
                fileName: "explorer.exe",
                arguments: documentDirectory.DirectoryPath
            );

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task DeleteDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
            var result = _messageBoxService.Show(title: "Remover Diretório de Documentos",
                                                 message: "Remover um diretório de documentos irá apagar todos os registros de documentos associados a este diretório da base de dados e do índice de pesquisa. Deseja continuar?",
                                                 buttons: MessageBoxButton.YesNo,
                                                 icon: MessageBoxIcon.Question);

            if (result == MessageBoxResult.Yes) {
                var response = await _mediator.Send(new DeleteDocumentDirectoryRequest {
                    DocumentDirectoryID = documentDirectory.ID
                });

                await ShowSnackbarAsync(
                    title: "Remover Directório de Documentos",
                    message: response.Succeeded()
                        ? "Diretório de Documentos removido com sucesso."
                        : $"Não foi remover o Diretório de Documentos. Erro: {response.Error}",
                    severity: response.Succeeded()
                        ? Severity.Success
                        : Severity.Warning
                );

                await UpdateViewModelAsync();
            }
        }

        [RelayCommand]
        private Task CreateNewDocumentDirectoryAsync() {
            _navigationService.Navigate(pageType: typeof(DocumentDirectoryFormPage),
                                        dataContext: null);

            return Task.CompletedTask;
        }

        #endregion
    }
}
