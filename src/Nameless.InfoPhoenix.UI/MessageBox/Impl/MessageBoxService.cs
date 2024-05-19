using System.Windows;
using MS_MessageBox = System.Windows.MessageBox;

namespace Nameless.InfoPhoenix.UI.MessageBox.Impl {
    public sealed class MessageBoxService : IMessageBoxService {
        #region MessageBoxService Members

        public MessageBoxResult Show(string title, string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxIcon icon = MessageBoxIcon.Information, object? owner = null) {
            var result = owner is Window window
                ? MS_MessageBox.Show(window, message, title, buttons.Convert(), icon.Convert())
                : MS_MessageBox.Show(message, title, buttons.Convert(), icon.Convert());

            return result.Convert();
        }

        #endregion
    }
}
