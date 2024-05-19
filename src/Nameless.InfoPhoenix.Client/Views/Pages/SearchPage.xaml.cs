using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages {
    public partial class SearchPage : INavigableView<SearchViewModel>, INavigationAware {
        #region Public Constructors

        public SearchPage(SearchViewModel viewModel) {
            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));

            InitializeComponent();
        }

        #endregion

        #region INavigableView<SearchViewModel> Members

        public SearchViewModel ViewModel { get; }

        #endregion

        #region Private Methods

        private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            Dispatcher.InvokeAsync(() => ViewModel.ExecuteSearchCommand.ExecuteAsync(new SearchTerm(args.QueryText, Suggested: false)));
        }

        private void ExecuteSearchHandler(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args) {
            Dispatcher.InvokeAsync(() => ViewModel.ExecuteSearchCommand.ExecuteAsync(new SearchTerm((string)args.SelectedItem, Suggested: true)));
        }

        #endregion

        #region INavigationAware Members

        public void OnNavigatedTo()
            => ViewModel.InitializeCommand.Execute(parameter: null);

        public void OnNavigatedFrom() {

        }

        #endregion
    }
}
