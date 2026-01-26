using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.DTOs;
using Nameless.InfoPhoenix.Models;
using Nameless.WPF;
using Nameless.WPF.Configuration;
using Nameless.WPF.Dialogs.Message;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Windows;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class SearchResultsSliderWindowViewModel : ViewModel {
    private readonly IMessageDialog _messageDialog;
    private readonly IWindowFactory _windowFactory;
    private readonly ILogger<SearchResultsSliderWindowViewModel> _logger;

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
    private DocumentDirectoryDto _documentDirectory = DocumentDirectoryDto.Empty;

    [ObservableProperty]
    private DocumentDto _currentDocument = DocumentDto.Empty;

    [ObservableProperty]
    private string _currentDocumentIcon = string.Empty;

    [ObservableProperty]
    private string[] _highlightTerms = [];

    [ObservableProperty]
    private bool _documentViewerEnabled;

    private int _currentIndex;
    private int _lastIndex;

    public SearchResultsSliderWindowViewModel(
        IAppConfigurationManager appConfigurationManager,
        IMessageDialog messageDialog,
        IWindowFactory windowFactory,
        ILogger<SearchResultsSliderWindowViewModel> logger
    ) {
        _messageDialog = messageDialog;
        _windowFactory = windowFactory;
        _logger = logger;

        DocumentViewerEnabled = appConfigurationManager.GetEnableDocumentViewer();
    }

    [RelayCommand]
    private Task InitializeAsync(SearchResultsMetadata metadata) {
        Title = metadata.DocumentDirectory.Label;
        DocumentDirectory = metadata.DocumentDirectory;
        HighlightTerms = metadata.HighlightTerms;

        _lastIndex = DocumentDirectory.Documents.Length - 1;

        SetCurrentDocument(Paginate.First);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenCurrentDocumentAsync() {
        var filePath = CurrentDocument.FilePath;
        if (!File.Exists(filePath)) {
            _messageDialog.ShowWarning(
                title: "Arquivo Não Localizado",
                message: "Não foi possível localizar o documento no sistema de arquivos."
            );

            return Task.CompletedTask;
        }

        try { ProcessHelper.OpenFile(filePath); }
        catch (Exception ex) {
            _messageDialog.ShowError(
                title: "Erro",
                message: $"Ocorreu um erro ao abrir o arquivo solicitado: Mensagem: {ex.Message}"
            );

            _logger.OpenCurrentDocumentError(CurrentDocument.FilePath, ex);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenDocumentViewerAsync() {
        if (_windowFactory.TryCreate<DocumentViewerWindow>(out var output)) {
            output.SetTitle(CurrentDocument.FileName);
            output.Initialize(CurrentDocument);
            output.Show(WindowStartupLocation.CenterScreen);
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

    private void UpdateCurrentIndex(Paginate paginate) {
        _currentIndex = paginate switch {
            Paginate.First => 0,
            Paginate.Previous => _currentIndex - 1 >= 0 ? _currentIndex - 1 : 0,
            Paginate.Next => _currentIndex + 1 <= _lastIndex ? _currentIndex + 1 : _lastIndex,
            Paginate.Last => _lastIndex,
            _ => 0
        };
    }

    private void UpdateCurrentDocument() {
        CurrentDocument = DocumentDirectory.Documents.ElementAtOrDefault(_currentIndex)
                          ?? DocumentDto.Empty;
        
        var documentFileExtension = Path.GetExtension(CurrentDocument.FilePath);

        CurrentDocumentIcon = $"/Resources/files/{documentFileExtension[1..]}_file.png";
    }

    private void UpdatePaginationFlags() {
        HasFirst = _currentIndex > 0;
        HasPrevious = _currentIndex > 0;
        HasNext = _currentIndex < _lastIndex;
        HasLast = _currentIndex < _lastIndex;
    }
}
