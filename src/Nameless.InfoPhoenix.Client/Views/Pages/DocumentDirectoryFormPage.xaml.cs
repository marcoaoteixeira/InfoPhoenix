using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages {
    public partial class DocumentDirectoryFormPage : INavigableView<DocumentDirectoryFormViewModel>, INavigationAware {
        #region Public Constructors

        public DocumentDirectoryFormPage(DocumentDirectoryFormViewModel viewModel) {
            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));

            InitializeComponent();
        }

        #endregion

        #region INavigableView<DocumentDirectoryFormViewModel> Members

        public DocumentDirectoryFormViewModel ViewModel { get; }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo() {
            if (DataContext is Guid documentDirectoryID) {
                ViewModel.FetchDocumentDirectoryCommand.Execute(documentDirectoryID);
            }
        }

        public void OnNavigatedFrom() {
            
        }

        #endregion
    }
}
