using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.Collections.Generic;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Client.Contracts.Views.Windows;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Domains.Dtos;
using Nameless.InfoPhoenix.Domains.UseCases.Search;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class SearchPageViewModel : ViewModelBase {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IMediator _mediator;
    private readonly IWindowFactory _windowFactory;

    private CircularBuffer<string>? _searchHistoryBuffer;
    
    [ObservableProperty]
    private string[] _searchHistory = [];

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _documentDirectories = [];

    private SearchResultCollection SearchResults { get; set; } = [];
    private string[] HighlightTerms { get; set; } = [];

    public SearchPageViewModel(IAppConfigurationManager appConfigurationManager,
                               IMediator mediator,
                               IWindowFactory windowFactory) {
        _appConfigurationManager = appConfigurationManager;
        _mediator = mediator;
        _windowFactory = windowFactory;
    }

    [RelayCommand]
    private Task UpdateAsync() {
        UpdateSearchHistoryBuffer();

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync(SearchTerm term) {
        var result = await _mediator.Send(new SearchDocumentsRequest {
            QueryTerm = term.Value
        });

        SearchResults = result.Value;
        HighlightTerms = result.HighlightTerms;

        DocumentDirectories = [.. result.Value.GetDocumentDirectories()];

        if (SearchResults.Count > 0) {
            UpdateSearchHistory(term);
        }
    }

    [RelayCommand]
    private Task ShowDocumentsAsync(DocumentDirectoryDto documentDirectory) {
        var documents = SearchResults.GetDocuments(documentDirectory.ID)
                                     .ToArray();

        if (_windowFactory.TryCreate<IDisplayDocumentSearchResultWindow>(owner: null, out var window)) {
            window.SetTitle($"Resultados da Pesquisa para: {documentDirectory.Label}");
            window.Initialize(documents, HighlightTerms);
            window.Show(StartupLocation.CenterScreen);
        }

        return Task.CompletedTask;
    }

    private void UpdateSearchHistory(SearchTerm term) {
        if (_searchHistoryBuffer is null || _searchHistoryBuffer.Contains(term.Value)) {
            return;
        }

        _searchHistoryBuffer.Add(term.Value);

        SearchHistory = [.. _searchHistoryBuffer];

        _appConfigurationManager.SetSearchHistory(SearchHistory);
    }

    private void UpdateSearchHistoryBuffer() {
        var capacity = (int)_appConfigurationManager.GetSearchHistoryLimit();
        var searchHistory = _searchHistoryBuffer?.ToArray() ??
                            _appConfigurationManager.GetSearchHistory();

        if (_searchHistoryBuffer is null || _searchHistoryBuffer.Capacity != capacity) {
            searchHistory = searchHistory.Take(capacity).ToArray();
            _searchHistoryBuffer = new CircularBuffer<string>(capacity, searchHistory);
            _appConfigurationManager.SetSearchHistory(searchHistory);
        }

        SearchHistory = searchHistory;
    }
}