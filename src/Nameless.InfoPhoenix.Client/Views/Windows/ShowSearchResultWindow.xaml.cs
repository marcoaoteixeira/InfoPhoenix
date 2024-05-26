using System.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.UI;

namespace Nameless.InfoPhoenix.Client.Views.Windows {
    public partial class ShowSearchResultWindow : IViewModelAware<ShowSearchResultWindowViewModel> {
        #region Public Constructors

        public ShowSearchResultWindow(ShowSearchResultWindowViewModel viewModel) {
            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));

            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void CloseHandler(object _, RoutedEventArgs __)
            => Close();

        #endregion

        #region IViewModelAware<ShowSearchResultWindowViewModel> Members

        public ShowSearchResultWindowViewModel ViewModel { get; }

        #endregion
    }
}
