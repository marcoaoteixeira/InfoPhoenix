using MS_MessageBoxImage = System.Windows.MessageBoxImage;

namespace Nameless.InfoPhoenix.UI.MessageBox {
    public enum MessageBoxIcon {
        None,
        Critical,
        Error,
        Exclamation,
        Information,
        Question,
        Warning,
    }

    public static class MessageBoxIconExtension {
        #region Public Static Methods

        public static MS_MessageBoxImage Convert(this MessageBoxIcon self)
            => self switch {
                MessageBoxIcon.None => MS_MessageBoxImage.None,
                MessageBoxIcon.Critical => MS_MessageBoxImage.Error,
                MessageBoxIcon.Error => MS_MessageBoxImage.Error,
                MessageBoxIcon.Exclamation => MS_MessageBoxImage.Exclamation,
                MessageBoxIcon.Information => MS_MessageBoxImage.Information,
                MessageBoxIcon.Question => MS_MessageBoxImage.Question,
                MessageBoxIcon.Warning => MS_MessageBoxImage.Warning,
                _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
            };

        #endregion
    }
}
