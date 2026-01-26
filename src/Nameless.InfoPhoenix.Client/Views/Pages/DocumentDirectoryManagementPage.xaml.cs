using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[ServiceLifetime(Lifetime = ServiceLifetime.Singleton)]
[NavigationViewItem(Title = "Diretório de Documentos", Icon = SymbolRegular.Folder48)]
public partial class DocumentDirectoryManagementPage : INavigableView<DocumentDirectoryManagementPageViewModel> {
    public DocumentDirectoryManagementPageViewModel ViewModel { get; }

    public DocumentDirectoryManagementPage(DocumentDirectoryManagementPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }
}
