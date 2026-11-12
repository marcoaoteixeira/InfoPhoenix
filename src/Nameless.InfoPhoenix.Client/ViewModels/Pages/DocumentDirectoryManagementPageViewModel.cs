using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.InfoPhoenix.Client.Features.FetchDocumentDirectories;
using Nameless.InfoPhoenix.Client.Infrastructure.UI.Contracts;
using Nameless.Mediator;
using Nameless.Windows.Helpers;
using Nameless.Windows.Localization;
using Nameless.Windows.Mvvm;
using Nameless.Windows.UI;
using Wpf.Ui.Abstractions.Controls;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages;

public partial class DocumentDirectoryManagementPageViewModel : ViewModel, INavigationAware {
    private readonly IMediator _mediator;
    private readonly ITranslator<DocumentDirectoryManagementPageViewModel> _translator;
    private readonly IWindowFactory _windowFactory;

    private bool _initialized;

    [ObservableProperty]
    private ObservableCollection<DocumentDirectoryDto> _entries = [];

    public DocumentDirectoryManagementPageViewModel(IMediator mediator, ITranslator<DocumentDirectoryManagementPageViewModel> translator, IWindowFactory windowFactory) {
        _mediator = Throws.When.Null(mediator);
        _translator = Throws.When.Null(translator);
        _windowFactory = Throws.When.Null(windowFactory);

        Initialize();
    }

    public Task OnNavigatedToAsync() {
        return UpdateViewModelAsync(CancellationToken.None);
    }

    public Task OnNavigatedFromAsync() {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task CreateNewDocumentDirectoryAsync() {
        var window = _windowFactory.Create<IDocumentDirectoryWindow>();

        window.SetTitle(_translator[""]);
        window.SetDocumentDirectory(Guid.Empty);

        window.Show(WindowStartupLocation.CenterScreen);
        
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

    private async Task UpdateViewModelAsync(CancellationToken cancellationToken) {
        var entries = await _mediator.ExecuteAsync(new FetchDocumentDirectoriesRequest(), cancellationToken)
                                     .SkipContextSync();

        Entries = [.. entries];
    }

    private void Initialize() {
        if (_initialized) { return; }

        SubscribeForEvents();

        _initialized = true;
    }

    private void SubscribeForEvents() { }
}
