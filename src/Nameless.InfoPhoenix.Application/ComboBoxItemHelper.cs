using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace Nameless.InfoPhoenix.Application;

public static class ComboBoxItemHelper {
    public static ComboBoxItem EmptyComboBoxItem => new() { Content = "Selecione" };

    public static ComboBoxItem Create<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => new() { Content = value.GetDescription(), Tag = value };

    public static bool TrySelect<TEnum>(ComboBoxItem[] items,
                                        TEnum value,
                                        [NotNullWhen(returnValue: true)] out ComboBoxItem? comboBoxItem)
        where TEnum : struct, Enum {
        comboBoxItem = items.FirstOrDefault(item => Equals(item.Tag, value));
        if (comboBoxItem is not null) {
            comboBoxItem.IsSelected = true;
        }
        return comboBoxItem is not null;
    }
}