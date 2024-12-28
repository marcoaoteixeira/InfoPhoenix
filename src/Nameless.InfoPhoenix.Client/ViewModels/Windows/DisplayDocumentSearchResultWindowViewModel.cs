using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Client.Contracts.Views.Windows;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Dialogs;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class DisplayDocumentSearchResultWindowViewModel : ViewModelBase {
    private readonly IDialogService _dialogService;
    private readonly IFileSystem _fileSystem;
    private readonly IWindowFactory _windowFactory;
    private readonly ILogger<DisplayDocumentSearchResultWindowViewModel> _logger;

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
    private string _documentIcon = string.Empty;

    [ObservableProperty]
    private string _documentFilePath = string.Empty;

    [ObservableProperty]
    private string _documentFileName = string.Empty;

    [ObservableProperty]
    private string _documentContent = string.Empty;
    
    [ObservableProperty]
    private bool _documentViewerEnabled;

    [ObservableProperty]
    private string[] _highlightTerms = [];

    private DocumentDto[] _documents = [];
    private DocumentDto _current = new();
    private int _currentIndex;
    private int _lastIndex;

    public DisplayDocumentSearchResultWindowViewModel(IAppConfigurationManager appConfigurationManager,
                                                      IDialogService dialogService,
                                                      IFileSystem fileSystem,
                                                      IWindowFactory windowFactory,
                                                      ILogger<DisplayDocumentSearchResultWindowViewModel> logger) {
        _dialogService = Prevent.Argument.Null(dialogService);
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _windowFactory = Prevent.Argument.Null(windowFactory);
        _logger = Prevent.Argument.Null(logger);

        DocumentViewerEnabled = Prevent.Argument.Null(appConfigurationManager).GetEnableDocumentViewer();
    }

    [RelayCommand]
    private Task InitializeAsync(SearchedDocuments searchedDocuments) {
        _documents = searchedDocuments.Documents;

        HighlightTerms = searchedDocuments.HighlightTerms;

        SetCurrentDocument(Paginate.First);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenCurrentDocumentAsync() {
        var filePath = _current.FilePath;
        if (!_fileSystem.File.Exists(filePath)) {
            _dialogService.ShowWarning(title: "Arquivo Não Localizado",
                                       message: "Não foi possível localizar o documento no sistema de arquivos.");

            return Task.CompletedTask;
        }

        try { ProcessHelper.OpenFile(filePath); }
        catch (Exception ex) {
            _dialogService.ShowError(title: "Erro",
                                     message: $"Ocorreu um erro ao abrir o arquivo solicitado: Mensagem: {ex.Message}");

            _logger.LogError(ex, "An error occurred while opening document. Error: {Error}", ex.Message);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenDocumentViewerAsync() {
        if (_windowFactory.TryCreate<IDocumentViewerWindow>(owner: null, out var documentViewer)) {
            documentViewer.SetTitle(_current.FilePath);
            documentViewer.SetDocumentFilePath(_current.FilePath);
            documentViewer.Show(StartupLocation.CenterScreen);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DisplayDocumentAsync(Paginate paginate) {
        SetCurrentDocument(paginate);

        return Task.CompletedTask;
    }

    private void SetCurrentDocument(Paginate paginate) {
        UpdateCurrentIndex(paginate);
        UpdateCurrentDocument();
        UpdatePaginationFlags();
    }

    private void UpdateCurrentDocument() {
        _current = _documents.ElementAtOrDefault(_currentIndex) ?? new DocumentDto();
        _lastIndex = _documents.Length - 1;

        var documentFileExtension = _fileSystem.Path.GetExtension(_current.FilePath) ?? ".doc";

        DocumentFilePath = _current.FilePath;
        DocumentFileName = _fileSystem.Path.GetFileName(_current.FilePath) ?? string.Empty;
        DocumentContent = _current.Content;
        DocumentIcon = $"/Resources/files/{documentFileExtension[1..]}_file.png";
    }

    private void UpdateCurrentIndex(Paginate paginate)
        => _currentIndex = paginate switch {
            Paginate.First => 0,
            Paginate.Previous => _currentIndex - 1 >= 0 ? _currentIndex - 1 : 0,
            Paginate.Next => _currentIndex + 1 <= _lastIndex ? _currentIndex + 1 : _lastIndex,
            Paginate.Last => _lastIndex,
            _ => 0
        };

    private void UpdatePaginationFlags() {
        HasFirst = _currentIndex > 0;
        HasPrevious = _currentIndex > 0;
        HasNext = _currentIndex < _lastIndex;
        HasLast = _currentIndex < _lastIndex;
    }
}