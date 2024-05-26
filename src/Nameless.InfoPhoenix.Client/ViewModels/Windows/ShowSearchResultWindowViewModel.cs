using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Client.Resources;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.UI;
using Nameless.InfoPhoenix.UI.MessageBox;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows {
    public partial class ShowSearchResultWindowViewModel : ViewModelBase {
        private readonly IWindowFactory _windowFactory;

        #region Private Read-Only Fields

        private readonly IMessageBoxService _messageBoxService;
        private readonly ILogger _logger;

        #endregion

        #region Private Fields

        private SearchResultEntryDto _current = SearchResultEntryDto.Empty;
        private int _currentIndex;
        private int _lastIndex;

        #endregion

        #region Private Fields for Observables

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _hasFirst;

        [ObservableProperty]
        private bool _hasPrevious;

        [ObservableProperty]
        private bool _hasNext;

        [ObservableProperty]
        private bool _hasLast;

        [ObservableProperty]
        private string _documentIcon = ResourceHelper.GetDocumentIcon(string.Empty);

        [ObservableProperty]
        private string _documentFileName = string.Empty;

        [ObservableProperty]
        private string _documentContent = string.Empty;

        [ObservableProperty]
        private int _documentContentFontSize = 16;

        #endregion

        #region Public Properties

        public SearchResultCollectionDto SearchResultCollection { get; private set; } = SearchResultCollectionDto.Empty;
        public string[] HighlightTerms { get; private set; } = [];

        #endregion

        #region Public Constructors

        public ShowSearchResultWindowViewModel(
            IMessageBoxService messageBoxService,
            ILogger<ShowSearchResultWindowViewModel> logger,
            IPubSubService pubSubService,
            IWindowFactory windowFactory) : base(pubSubService) {
            _windowFactory = windowFactory;
            _messageBoxService = Guard.Against.Null(messageBoxService, nameof(messageBoxService));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        #endregion

        #region Public Methods

        public void SetSearchResultCollection(SearchResultCollectionDto value) {
            SearchResultCollection = value;

            _currentIndex = 0;
            _lastIndex = 0;
        }

        public void SetHighlightTerms(string[] value)
            => HighlightTerms = value;

        public void Initialize() {
            UpdateCurrentDocument();
            UpdateCurrentIndex(PaginationAction.First);
            UpdatePagination();
        }

        #endregion

        #region Private Methods

        private void UpdateCurrentIndex(PaginationAction action)
            => _currentIndex = action switch {
                PaginationAction.First => 0,
                PaginationAction.Previous => _currentIndex - 1 >= 0 ? _currentIndex - 1 : 0,
                PaginationAction.Next => _currentIndex + 1 <= _lastIndex ? _currentIndex + 1 : _lastIndex,
                PaginationAction.Last => _lastIndex,
                _ => 0
            };

        private void UpdateCurrentDocument() {
            _current = SearchResultCollection.ElementAtOrDefault(_currentIndex) ?? SearchResultEntryDto.Empty;
            _lastIndex = SearchResultCollection.Count - 1;

            Title = $"Resultados da Pesquisa em: {_current.DocumentDirectoryLabel}";
            DocumentIcon = ResourceHelper.GetDocumentIcon(_current.DocumentFilePath);
            DocumentFileName = _current.DocumentFileName;
            DocumentContent = _current.DocumentContent;
        }

        private void UpdatePagination() {
            HasFirst = _currentIndex > 0;
            HasPrevious = _currentIndex > 0;
            HasNext = _currentIndex < _lastIndex;
            HasLast = _currentIndex < _lastIndex;
        }

        #endregion

        #region Private Methods (Commands)

        [RelayCommand]
        private Task DisplaySearchResultEntryAsync(PaginationAction action) {
            UpdateCurrentIndex(action);
            UpdatePagination();
            UpdateCurrentDocument();

            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task ChangeContentFontSizeAsync(FontGrowthAction fontGrowth) {
            const int CHANGE = 2;
            const int MIN_VALUE = 10;
            const int MAX_VALUE = 40;

            DocumentContentFontSize = fontGrowth switch {
                FontGrowthAction.Increase => DocumentContentFontSize + CHANGE < MAX_VALUE
                    ? DocumentContentFontSize + CHANGE
                    : MAX_VALUE,

                FontGrowthAction.Decrease => DocumentContentFontSize - CHANGE > MIN_VALUE
                    ? DocumentContentFontSize - CHANGE
                    : MIN_VALUE,

                _ => DocumentContentFontSize
            };

            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task OpenCurrentDocumentAsync(object? parameter) {
            var filePath = _current.DocumentFilePath;
            if (!File.Exists(filePath)) {
                _messageBoxService.Show(title: "Arquivo Não Localizado",
                                        message: "Não foi possível localizar o documento no sistema de arquivos.",
                                        icon: MessageBoxIcon.Exclamation);
                return Task.CompletedTask;
            }

            try {
                using var process = Process.Start(
                    fileName: filePath
                );
            }
            catch (Exception ex) {
                _messageBoxService.Show(title: "Erro",
                                        message: $"Ocorreu um erro ao abrir o arquivo solicitado: Mensagem: {ex.Message}",
                                        icon: MessageBoxIcon.Error);
                _logger.LogError(ex, "Error while opening document. Message: {Error}", ex.Message);
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task OpenDocumentViewerAsync(object? parameter) {
            _windowFactory.DisplayDocumentViewer(_current.DocumentFilePath);

            return Task.CompletedTask;
        }

        #endregion
    }
}
