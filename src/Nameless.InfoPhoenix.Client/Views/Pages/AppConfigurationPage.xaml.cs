using Nameless.InfoPhoenix.Client.Resources.l10n;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[NavigationViewItem(
    Icon = SymbolRegular.Settings48,
    Placement = Placement.Footer,
    TitleResourceName = "AppConfigurationPage_NavigationViewItem_Title",
    ResourceType = typeof(Localization)
)]
public partial class AppConfigurationPage : INavigableView<AppConfigurationPageViewModel> {
    public AppConfigurationPageViewModel ViewModel { get; }

    public AppConfigurationPage(AppConfigurationPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }
}
