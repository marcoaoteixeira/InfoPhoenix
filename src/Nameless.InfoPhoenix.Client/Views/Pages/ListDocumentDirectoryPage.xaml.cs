using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages {
    public partial class ListDocumentDirectoryPage : INavigableView<ListDocumentDirectoryViewModel>, INavigationAware {
        #region Public Constructors

        public ListDocumentDirectoryPage(ListDocumentDirectoryViewModel viewModel) {
            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));

            InitializeComponent();
        }

        #endregion

        #region INavigableView<ListDocumentDirectoryViewModel> Members

        public ListDocumentDirectoryViewModel ViewModel { get; }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo()
            => ViewModel.UpdateViewModelCommand.Execute(parameter: null);

        public void OnNavigatedFrom() { }

        #endregion
    }
}
