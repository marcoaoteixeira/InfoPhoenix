using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[NavigationViewItem(
    Icon = SymbolRegular.Settings48,
    Title = Constants.Labels.NavigationMenuItems.Configuration,
    Placement = Placement.Bottom
)]
public partial class AppConfigurationPage : INavigableView<AppConfigurationPageViewModel> {
    public AppConfigurationPageViewModel ViewModel { get; }

    public AppConfigurationPage(AppConfigurationPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }
}
