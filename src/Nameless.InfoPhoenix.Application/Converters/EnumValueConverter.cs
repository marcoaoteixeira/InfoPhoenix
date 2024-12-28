using System.Globalization;
using System.Windows.Data;

namespace Nameless.InfoPhoenix.Application.Converters;

public abstract class EnumValueConverter<TEnum> : IValueConverter
    where TEnum : struct, Enum {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TEnum enumValue
            ? enumValue
            : default;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string enumValue && Enum.TryParse<TEnum>(enumValue, out var result)
            ? result
            : default;
}