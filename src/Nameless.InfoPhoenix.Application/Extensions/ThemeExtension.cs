using System.Windows.Controls;
using Nameless.InfoPhoenix.Configuration;
using Wpf.Ui.Appearance;

namespace Nameless.InfoPhoenix.Application;

public static class ThemeExtension {
    public static ApplicationTheme ToApplicationTheme(this Theme self)
        => self switch {
            Theme.Light => ApplicationTheme.Light,
            Theme.Dark => ApplicationTheme.Dark,
            Theme.HighContrast => ApplicationTheme.HighContrast,
            _ => ApplicationTheme.Light
        };

    public static ComboBoxItem ToComboBoxItem(this Theme self)
        => ComboBoxItemHelper.Create(self);

    public static ComboBoxItem GetComboBoxItemFromAvailable(this Theme self, ComboBoxItem[] items)
        => ComboBoxItemHelper.TrySelect(items, self, out var comboBoxItem)
        ? comboBoxItem
        : ComboBoxItemHelper.EmptyComboBoxItem;
}