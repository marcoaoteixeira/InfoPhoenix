using MS_MessageBoxResult = System.Windows.MessageBoxResult;

namespace Nameless.InfoPhoenix.UI.MessageBox {
    public enum MessageBoxResult {
        OK,
        No,
        Yes,
        Cancel
    }

    public static class MessageBoxResultExtension {
        #region Public Static Methods

        public static MessageBoxResult Convert(this MS_MessageBoxResult self)
            => self switch {
                MS_MessageBoxResult.OK => MessageBoxResult.OK,
                MS_MessageBoxResult.No => MessageBoxResult.No,
                MS_MessageBoxResult.Yes => MessageBoxResult.Yes,
                MS_MessageBoxResult.Cancel => MessageBoxResult.Cancel,
                _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
            };

        #endregion
    }
}
