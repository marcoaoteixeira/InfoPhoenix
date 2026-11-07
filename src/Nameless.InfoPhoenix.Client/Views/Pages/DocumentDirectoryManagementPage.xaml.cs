using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[NavigationViewItem(Title = "Diretório de Documentos", Icon = SymbolRegular.Folder48)]
public partial class DocumentDirectoryManagementPage : INavigableView<DocumentDirectoryManagementPageViewModel> {
    public DocumentDirectoryManagementPageViewModel ViewModel { get; }

    public DocumentDirectoryManagementPage(DocumentDirectoryManagementPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }
}
