using System.Globalization;
using System.Windows.Data;

namespace Nameless.InfoPhoenix.Application.Converters;

public sealed class DateTimeToStringValueConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime dateTime
            ? string.Format(culture, "{0:G}", dateTime)
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string dateTime && DateTime.TryParse(dateTime, culture, out var result)
            ? result
            : null;
}