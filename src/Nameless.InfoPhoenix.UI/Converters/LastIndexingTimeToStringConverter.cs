using System.Globalization;
using System.Windows.Data;

namespace Nameless.InfoPhoenix.UI.Converters {
    public sealed class LastIndexingTimeToStringConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is DateTime lastIndexingTime
                ? $"{lastIndexingTime:G}"
                : string.Empty;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string lastIndexingTime && DateTime.TryParse(lastIndexingTime, out var result)
                ? result
                : null;

        #endregion
    }
}
