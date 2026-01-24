using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.DTOs;
using Nameless.InfoPhoenix.Models;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Windows;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

[ServiceLifetime(Lifetime = ServiceLifetime.Transient)]
public partial class SearchResultsSliderWindow : ISearchResultsSliderWindow, IHasViewModel<SearchResultsSliderWindowViewModel> {
    public SearchResultsSliderWindowViewModel ViewModel { get; }

    public SearchResultsSliderWindow(SearchResultsSliderWindowViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }

    public void SetTitle(string title) {
        ViewModel.Title = title;
    }

    public void Initialize(DocumentDirectoryDto documentDirectory, string[] highlightTerms) {
        ViewModel.InitializeCommand.Execute(new SearchResultsMetadata(
            documentDirectory,
            highlightTerms
        ));
    }

    public void Show(WindowStartupLocation startupLocation) {
        if (Application.Current.MainWindow is not null) {
            Owner = Application.Current.MainWindow;
        }

        WindowStartupLocation = startupLocation;

        Show();
    }

    private void CloseHandler(object _, RoutedEventArgs __) {
        Close();
    }
}

public interface ISearchResultsSliderWindow : IWindow {
    void SetTitle(string title);

    void Initialize(DocumentDirectoryDto documentDirectory, string[] highlightTerms);
}