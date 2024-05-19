using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.UI.Impl {
    public sealed class PageService : IPageService {
        #region Private Read-Only Fields

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Public Constructors

        public PageService(IServiceProvider serviceProvider) {
            _serviceProvider = Guard.Against.Null(serviceProvider, nameof(serviceProvider));
        }

        #endregion

        #region IPageService Members

        public T? GetPage<T>() where T : class
            => GetPage(typeof(T)) as T;

        public FrameworkElement? GetPage(Type pageType) {
            if (typeof(FrameworkElement).IsAssignableFrom(pageType)) {
                return (FrameworkElement)_serviceProvider.GetRequiredService(pageType);
            }

            throw new InvalidOperationException("Page should be a WPF control.");
        }

        #endregion
    }
}
