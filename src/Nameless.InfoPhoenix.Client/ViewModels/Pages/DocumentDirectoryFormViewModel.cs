using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.InfoPhoenix.Client.Extensions;
using Nameless.InfoPhoenix.Client.Models;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Objects;
using Nameless.InfoPhoenix.UI;
using Nameless.InfoPhoenix.UI.MessageBox;
using Ookii.Dialogs.Wpf;
using Wpf.Ui;
using MessageBoxButton = Nameless.InfoPhoenix.UI.MessageBox.MessageBoxButton;
using MessageBoxResult = Nameless.InfoPhoenix.UI.MessageBox.MessageBoxResult;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages {
    public partial class DocumentDirectoryFormViewModel : ViewModelBase {
        #region Private Read-Only Fields

        private readonly IMediator _mediator;
        private readonly IMessageBoxService _messageBoxService;
        private readonly INavigationService _navigationService;

        #endregion

        #region Private Fields

        private CancellationTokenSource? _cancellationTokenSource;
        private bool _running;

        #endregion

        #region Private Fields (Observables)

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private DocumentDirectoryModel _currentDocumentDirectory = new();

        #endregion

        #region Public Constructors

        public DocumentDirectoryFormViewModel(
            IMediator mediator,
            IMessageBoxService messageBoxService,
            INavigationService navigationService,
            IPubSubService pubSubService) : base(pubSubService) {
            _mediator = Guard.Against.Null(mediator, nameof(mediator));
            _messageBoxService = Guard.Against.Null(messageBoxService, nameof(messageBoxService));
            _navigationService = Guard.Against.Null(navigationService, nameof(navigationService));
        }

        #endregion

        #region Private Methods (Commands)

        [RelayCommand]
        private async Task FetchDocumentDirectoryAsync(Guid documentDirectoryID) {
            var response = await _mediator.Send(new FetchDocumentDirectoryRequest {
                DocumentDirectoryID = documentDirectoryID
            });

            CurrentDocumentDirectory = response.Value.ToModel();
            Title = CurrentDocumentDirectory.ID != Guid.Empty
                ? "Editar Diretório de Documentos"
                : "Criar Novo Diretório de Documentos";
        }

        [RelayCommand]
        private Task PickDirectoryPathAsync() {
            var dialog = new VistaFolderBrowserDialog {
                Multiselect = false
            };

            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result.GetValueOrDefault()) {
                CurrentDocumentDirectory.DirectoryPath = dialog.SelectedPath;
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SaveAsync() {
            if (_running) { return; }

            _running = true;

            var response = await _mediator.Send(new SaveDocumentDirectoryRequest {
                DocumentDirectory = CurrentDocumentDirectory.ToDto()
            });

            await PubSubService
                .PublishAsync(new SnackbarNotification {
                    Title = "Diretório de Documentos",
                    Message = response.Succeeded()
                        ? $"Diretório de Documentos [{response.Value?.DirectoryName}] salvo com sucesso."
                        : response.Error,
                    Severity = response.Succeeded()
                        ? Severity.Success
                        : Severity.Error
                });

            NavigatePrevious();
        }

        [RelayCommand]
        private Task CancelAsync() {
            if (_running) {
                var result = _messageBoxService.Show(title: "Cancelar", 
                                                     message: "Cancelar a operação pode acarretar na perda de dados. Deseja continuar?",
                                                     buttons: MessageBoxButton.YesNo,
                                                     icon: MessageBoxIcon.Exclamation);
                
                if (result == MessageBoxResult.Yes) {
                    GetCancellationTokenSource().Cancel();
                }

                return Task.CompletedTask;
            }

            NavigatePrevious();

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void NavigatePrevious() {
            ResetCancellationTokenSource();

            _navigationService.GoBack();

            _running = false;
        }

        private CancellationTokenSource GetCancellationTokenSource() {
            if (_cancellationTokenSource is null || _cancellationTokenSource.IsCancellationRequested) {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            return _cancellationTokenSource;
        }

        private void ResetCancellationTokenSource() {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        #endregion
    }
}
