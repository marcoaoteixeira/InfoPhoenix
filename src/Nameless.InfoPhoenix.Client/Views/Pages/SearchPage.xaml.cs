using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[NavigationViewItem(Title = "Pesquisar", Icon = SymbolRegular.DocumentSearch24)]
public partial class SearchPage : INavigableView<SearchPageViewModel> {
    public SearchPageViewModel ViewModel { get; }

    public SearchPage(SearchPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }

    private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
        
    }

    private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args) {
        
    }
}
