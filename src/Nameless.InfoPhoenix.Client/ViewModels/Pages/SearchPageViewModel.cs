using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.Collections.Generic;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.DTOs;
using Nameless.InfoPhoenix.UseCases.Search;
using Nameless.Mediator;
using Nameless.WPF.Configuration;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class SearchPageViewModel : ViewModel, INavigationAware {
    private readonly IAppConfigurationManager _appConfigurationManager;
    private readonly IMediator _mediator;
    private readonly IWindowFactory _windowFactory;

    private SearchHistoryLimit _currentSearchHistoryLimit;
    private string _previousSearchTerm = string.Empty;
    private CircularBuffer<string> _searchHistoryBuffer = new((int)SearchHistoryLimit.Small);

    [ObservableProperty]
    private ObservableCollection<string> _searchHistory = [];

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _entries = [];

    [ObservableProperty]
    private ObservableCollection<object> _multiSelectSelectedItems = [];

    private string[] HighlightTerms { get; set; } = [];

    public SearchPageViewModel(
        IAppConfigurationManager appConfigurationManager,
        IMediator mediator,
        IWindowFactory windowFactory) {
        _appConfigurationManager = appConfigurationManager;
        _mediator = mediator;
        _windowFactory = windowFactory;
    }

    public Task OnNavigatedToAsync() {
        RefreshSearchHistory();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync() {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync(string searchTerm, CancellationToken cancellationToken) {
        if (_previousSearchTerm.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)) { return; }

        _previousSearchTerm = searchTerm;

        var response = await _mediator.ExecuteAsync(
            new SearchDocumentsRequest { Term = searchTerm },
            cancellationToken
        );

        response.Match(
            onSuccess: value => {
                Entries = [.. value.DocumentDirectories];
                HighlightTerms = value.HighlightTerms;

                UpdateSearchHistory(value.SearchTerm);
            },
            onFailure: _ => { }
        );
    }

    [RelayCommand]
    private Task ShowDocumentsAsync(DocumentDirectoryDto entry) {
        if (_windowFactory.TryCreate<SearchResultsSliderWindow>(out var output)) {
            output.SetTitle(entry.Label);
            output.Initialize(entry, HighlightTerms);
            output.Show(WindowStartupLocation.CenterScreen);
        }

        return Task.CompletedTask;
    }

    private void RefreshSearchHistory() {
        var searchHistoryLimit = _appConfigurationManager.GetSearchHistoryLimit();

        // If the limit hasn't changed, no need to refresh
        if (searchHistoryLimit == _currentSearchHistoryLimit) { return;}

        // If the limit changed to None, clear the history
        if (searchHistoryLimit == SearchHistoryLimit.None) {
            _searchHistoryBuffer.Clear();
            _appConfigurationManager.SetSearchHistory([]);
            _currentSearchHistoryLimit = SearchHistoryLimit.None;

            SearchHistory.Clear();

            return;
        }

        // Update the buffer with the new limit
        _currentSearchHistoryLimit = searchHistoryLimit;

        var searchHistory = _appConfigurationManager.GetSearchHistory();

        _searchHistoryBuffer = new CircularBuffer<string>(
            capacity: (int)_currentSearchHistoryLimit,
            items: searchHistory
        );

        SearchHistory = [.. searchHistory];
    }

    private void UpdateSearchHistory(string searchTerm) {
        // If search history is disabled, do nothing
        if (_currentSearchHistoryLimit == SearchHistoryLimit.None) { return; }

        // If the term already exists, do nothing
        if (_searchHistoryBuffer.Contains(searchTerm, StringComparer.OrdinalIgnoreCase)) { return; }

        // Add the new term to the buffer
        _searchHistoryBuffer.Add(searchTerm);

        // Update the observable collection and persist the changes
        string[] newSearchHistory = [.. _searchHistoryBuffer];

        SearchHistory = [.. newSearchHistory];

        _appConfigurationManager.SetSearchHistory(newSearchHistory);
    }
}
