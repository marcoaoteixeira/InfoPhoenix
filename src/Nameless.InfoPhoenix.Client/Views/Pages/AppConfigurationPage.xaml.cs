using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Pages;

[ServiceLifetime(Lifetime = ServiceLifetime.Singleton)]
[NavigationViewItem(Title = "Configurações", Icon = SymbolRegular.Settings48, Footer = true)]
public partial class AppConfigurationPage : INavigableView<AppConfigurationPageViewModel> {
    public AppConfigurationPageViewModel ViewModel { get; }

    public AppConfigurationPage(AppConfigurationPageViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }
}
