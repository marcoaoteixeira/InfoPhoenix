using System.Windows;

namespace Nameless.InfoPhoenix.UI {
    public interface IWindowService {
        #region Methods

        void Show<TWindow>(Window? owner = null, object? dataContext = null)
            where TWindow : Window;

        bool? ShowDialog<TWindow>(Window? owner = null, object ? dataContext = null)
            where TWindow : Window;

        #endregion
    }
}
