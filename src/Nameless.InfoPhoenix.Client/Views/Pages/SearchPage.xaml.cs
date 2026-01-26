using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[ServiceLifetime(Lifetime = ServiceLifetime.Singleton)]
[NavigationViewItem(Title = "Pesquisar", Icon = SymbolRegular.DocumentSearch24)]
public partial class SearchPage : INavigableView<SearchPageViewModel> {
    public SearchPageViewModel ViewModel { get; }

    public SearchPage(SearchPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }

    private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
        Dispatcher.InvokeAsync(() => ViewModel.ExecuteSearchCommand.ExecuteAsync(args.QueryText));
    }

    private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args) {
        Dispatcher.InvokeAsync(() => ViewModel.ExecuteSearchCommand.ExecuteAsync((string)args.SelectedItem));
    }
}
