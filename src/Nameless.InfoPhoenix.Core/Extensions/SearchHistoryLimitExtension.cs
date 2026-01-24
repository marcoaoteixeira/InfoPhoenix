using System.Windows.Controls;
using Nameless.InfoPhoenix.Configuration;
using Nameless.WPF.Helpers;

namespace Nameless.InfoPhoenix;

public static class SearchHistoryLimitExtension {
    extension(SearchHistoryLimit self) {
        public ComboBoxItem ToComboBoxItem() {
            return ComboBoxItemHelper.Create(self, SearchHistoryLimitExtensions.GetDisplayText);
        }

        public ComboBoxItem GetComboBoxItem(ComboBoxItem[] available) {
            return ComboBoxItemHelper.TrySelect(available, self, out var output)
                ? output
                : ComboBoxItemHelper.EmptyComboBoxItem;
        }
    }
}