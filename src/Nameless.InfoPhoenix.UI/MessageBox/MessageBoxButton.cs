using MS_MessageBoxButton = System.Windows.MessageBoxButton;

namespace Nameless.InfoPhoenix.UI.MessageBox {
    public enum MessageBoxButton {
        OK,
        YesNo,
        YesNoCancel,
        OKCancel,
    }

    public static class MessageBoxButtonExtension {
        #region Public Static Methods

        public static MS_MessageBoxButton Convert(this MessageBoxButton self)
            => self switch {
                MessageBoxButton.OK => MS_MessageBoxButton.OK,
                MessageBoxButton.OKCancel => MS_MessageBoxButton.OKCancel,
                MessageBoxButton.YesNoCancel => MS_MessageBoxButton.YesNoCancel,
                MessageBoxButton.YesNo => MS_MessageBoxButton.YesNo,

                _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
            };

        #endregion
    }
}
