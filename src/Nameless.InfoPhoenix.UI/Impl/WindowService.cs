using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WinMessageBox = System.Windows.MessageBox;

namespace Nameless.InfoPhoenix.UI.Impl {
    public sealed class WindowService : IWindowService {
        #region Private Read-Only Fields

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Public Constructors

        public WindowService(IServiceProvider serviceProvider) {
            _serviceProvider = Guard.Against.Null(serviceProvider, nameof(serviceProvider));
        }

        #endregion

        #region Private Methods

        private bool TryCreateWindow<T>(Window? owner, object? dataContext, [NotNullWhen(returnValue: true)] out T? window) where T : Window {
            window = _serviceProvider.GetService<T>();

            if (window is null) {
                WinMessageBox.Show(owner: Application.Current.MainWindow!,
                                   caption: "Serviço Indisponível",
                                   messageBoxText: "O serviço requisitado não foi registrado no contêiner.",
                                   button: MessageBoxButton.OK,
                                   icon: MessageBoxImage.Warning);

                return false;
            }

            window.Owner = owner ?? Application.Current.MainWindow;
            window.DataContext = dataContext;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return true;
        }

        #endregion

        #region IWindowManager Members

        public void Show<T>(Window? owner = null, object? dataContext = null) where T : Window {
            if (TryCreateWindow<T>(owner, dataContext, out var window)) {
                window.DataContext = dataContext;
                window.Show();
            }
        }

        public bool? ShowDialog<T>(Window? owner = null, object? dataContext = null) where T : Window
            => TryCreateWindow<T>(owner, dataContext, out var window)
                ? window.ShowDialog()
                : null;

        #endregion
    }
}
