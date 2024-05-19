using System.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.UI;

namespace Nameless.InfoPhoenix.Client {
    public static class WindowServiceExtension {
        #region Public Static Methods

        public static void DisplaySearchResultWindow(this IWindowService _, SearchResultEntryGroupDto result)
            => new ShowSearchResultGroupWindow(new ShowSearchResultGroupWindowViewModel(result)) {
                Owner = Application.Current.MainWindow
            }.Show();

        #endregion
    }
}
