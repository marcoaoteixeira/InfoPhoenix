using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

public partial class AppConfigurationPage : INavigableView<AppConfigurationPageViewModel> {
    public static readonly string PageName = "Configurações";
    public static readonly string PageToolTip = "Configurações";

    public AppConfigurationPageViewModel ViewModel { get; }

    public AppConfigurationPage(AppConfigurationPageViewModel viewModel) {
        ViewModel = Prevent.Argument.Null(viewModel);

        DataContext = ViewModel;

        InitializeComponent();
    }
}