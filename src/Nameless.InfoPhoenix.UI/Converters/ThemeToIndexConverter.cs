using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace Nameless.InfoPhoenix.UI.Converters {
    public sealed class ThemeToIndexConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is ApplicationTheme theme
                ? theme
                : ApplicationTheme.Unknown;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is ApplicationTheme theme
                ? theme
                : ApplicationTheme.Unknown;

        #endregion
    }
}
