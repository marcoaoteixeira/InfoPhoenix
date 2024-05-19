using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages {
    public partial class AppConfigurationPage : INavigableView<AppConfigurationViewModel> {
        #region Public Constructors

        public AppConfigurationPage(AppConfigurationViewModel viewModel) {
            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));

            DataContext = ViewModel;

            InitializeComponent();
        }

        #endregion

        #region INavigableView<AppConfigurationViewModel> Members

        public AppConfigurationViewModel ViewModel { get; }

        #endregion
    }
}
