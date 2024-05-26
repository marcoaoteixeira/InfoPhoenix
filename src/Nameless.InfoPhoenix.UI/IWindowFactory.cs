using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace Nameless.InfoPhoenix.UI {
    public interface IWindowFactory {
        #region Methods

        bool TryCreate<TWindow>(Window? owner, object? dataContext, [NotNullWhen(returnValue: true)]out TWindow? window)
            where TWindow : Window;

        TWindow Create<TWindow>(Window? owner, params object[] parameters)
            where TWindow : Window;

        #endregion
    }
}
