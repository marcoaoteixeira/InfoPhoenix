using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Text;
using Nameless.InfoPhoenix.UI.Impl;
using Nameless.InfoPhoenix.UI.MessageBox;
using Nameless.InfoPhoenix.UI.MessageBox.Impl;
using Nameless.InfoPhoenix.UI.Text.Impl;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.UI.Extensions {
    public static class ServiceCollectionExtension {
        #region Public Static Methods

        public static IServiceCollection RegisterMessageBoxService(this IServiceCollection self)
            => self.AddSingleton<IMessageBoxService, MessageBoxService>();

        public static IServiceCollection RegisterNavigationService(this IServiceCollection self)
            => self.AddSingleton<INavigationService, NavigationService>();

        public static IServiceCollection RegisterPageService(this IServiceCollection self)
            => self.AddSingleton<IPageService, PageService>();
        
        public static IServiceCollection RegisterSnackbarService(this IServiceCollection self)
            => self.AddSingleton<ISnackbarService, SnackbarService>();

        public static IServiceCollection RegisterXPSDocumentReader(this IServiceCollection self)
            => self.AddSingleton<IDocumentReader, XPSDocumentReader>();

        public static IServiceCollection RegisterWindowFactory(this IServiceCollection self)
            => self.AddSingleton<IWindowFactory, WindowFactory>();

        #endregion
    }
}
