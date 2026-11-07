using System.Windows;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.InfoPhoenix.Client.Infrastructure.Objects;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class SearchResultsSliderWindow : ISearchResultsSliderWindow {
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
        if (System.Windows.Application.Current.MainWindow is not null) {
            Owner = System.Windows.Application.Current.MainWindow;
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