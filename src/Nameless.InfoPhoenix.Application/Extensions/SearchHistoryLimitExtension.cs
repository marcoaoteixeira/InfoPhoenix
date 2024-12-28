using System.Windows.Controls;
using Nameless.InfoPhoenix.Configuration;

namespace Nameless.InfoPhoenix.Application;

public static class SearchHistoryLimitExtension {
    public static ComboBoxItem ToComboBoxItem(this SearchHistoryLimit self)
        => ComboBoxItemHelper.Create(self);

    public static ComboBoxItem GetComboBoxItemFromAvailable(this SearchHistoryLimit self, ComboBoxItem[] items)
        => ComboBoxItemHelper.TrySelect(items, self, out var comboBoxItem)
            ? comboBoxItem
            : ComboBoxItemHelper.EmptyComboBoxItem;
}