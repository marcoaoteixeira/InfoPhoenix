using System.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.InfoPhoenix.Domains.UseCases.Search;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

public partial class SearchPage : INavigableView<SearchPageViewModel>, INavigationAware {
    public static readonly string PageName = "Pesquisar";
    public static readonly string PageToolTip = "Pesquisar";

    public SearchPageViewModel ViewModel { get; }

    public SearchPage(SearchPageViewModel pageViewModel) {
        ViewModel = Prevent.Argument.Null(pageViewModel);

        DataContext = ViewModel;

        InitializeComponent();
    }

    public void OnNavigatedTo()
        => ViewModel.UpdateCommand.Execute(parameter: null);

    public void OnNavigatedFrom() { }

    private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        => Dispatcher.InvokeAsync(() => ViewModel.ExecuteSearchCommand.ExecuteAsync(new SearchTerm(args.QueryText, suggested: false)));

    private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        => Dispatcher.InvokeAsync(() => ViewModel.ExecuteSearchCommand.ExecuteAsync(new SearchTerm((string)args.SelectedItem, suggested: true)));
}