using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.Windows.Helpers;
using Nameless.Windows.Mvvm;
using Wpf.Ui.Abstractions.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class DocumentDirectoryManagementPageViewModel : ViewModel, INavigationAware {
    private bool _initialized;

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _entries = [];

    public DocumentDirectoryManagementPageViewModel() {
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
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DoubleClickDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        Clipboard.SetText(documentDirectory.DirectoryPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task EditDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task FetchDocumentsForDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task IndexDocumentsInDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenDocumentDirectoryInExplorerAsync(DocumentDirectoryDto documentDirectory) {
        ProcessHelper.OpenDirectory(documentDirectory.DirectoryPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteDocumentDirectoryAsync(DocumentDirectoryDto documentDirectory) {
        return Task.CompletedTask;
    }

    private Task UpdateViewModelAsync() {
        return Task.CompletedTask;
    }

    private void Initialize() {
        if (_initialized) { return; }

        SubscribeForNotification();

        _initialized = true;
    }

    private void SubscribeForNotification() { }
}
