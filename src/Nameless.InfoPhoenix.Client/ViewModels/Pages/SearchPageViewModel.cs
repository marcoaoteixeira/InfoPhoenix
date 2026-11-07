using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.Collections.Generic;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.InfoPhoenix.Client.Infrastructure.Objects;
using Nameless.Windows.Mvvm;
using Wpf.Ui.Abstractions.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class SearchPageViewModel : ViewModel, INavigationAware {
    private string _previousSearchTerm = string.Empty;
    private CircularBuffer<string> _searchHistoryBuffer = new((int)SearchHistoryLimit.Small);

    [ObservableProperty]
    private ObservableCollection<string> _searchHistory = [];

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _entries = [];

    [ObservableProperty]
    private ObservableCollection<object> _multiSelectSelectedItems = [];

    private string[] HighlightTerms { get; set; } = [];

    public SearchPageViewModel() { }

    public Task OnNavigatedToAsync() {
        RefreshSearchHistory();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync() {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ExecuteSearchAsync(string searchTerm, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ShowDocumentsAsync(DocumentDirectoryDto entry) {
        return Task.CompletedTask;
    }

    private void RefreshSearchHistory() {

    }
}
