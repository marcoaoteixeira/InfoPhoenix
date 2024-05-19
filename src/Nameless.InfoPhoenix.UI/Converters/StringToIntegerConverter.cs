using System.Globalization;
using System.Windows.Data;

namespace Nameless.InfoPhoenix.UI.Converters {
    public sealed class StringToIntegerConverter : IValueConverter {
        #region IValueConverter Members

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is not int intValue) {
                return null;
            }

            return intValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is not string strValue) {
                return null;
            }

            if (int.TryParse(strValue, out var intValue)) {
                return intValue;
            }

            return null;
        } 

        #endregion
    }
}
