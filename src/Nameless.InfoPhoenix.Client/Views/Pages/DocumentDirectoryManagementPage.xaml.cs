using Nameless.InfoPhoenix.Client.Resources.l10n;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[NavigationViewItem(
    Icon = SymbolRegular.Folder48,
    Placement = Placement.Menu,
    TitleResourceName = "DocumentDirectoryManagementPage_NavigationViewItem_Title",
    ResourceType = typeof(Localization)
)]
public partial class DocumentDirectoryManagementPage : INavigableView<DocumentDirectoryManagementPageViewModel> {
    public DocumentDirectoryManagementPageViewModel ViewModel { get; }

    public DocumentDirectoryManagementPage(DocumentDirectoryManagementPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }
}
