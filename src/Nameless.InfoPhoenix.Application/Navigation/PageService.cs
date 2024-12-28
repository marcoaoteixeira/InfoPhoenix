using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Application.Navigation;

public sealed class PageService : IPageService {
    private readonly IServiceProvider _provider;

    public PageService(IServiceProvider provider) {
        _provider = Prevent.Argument.Null(provider);
    }

    public T? GetPage<T>() where T : class
        => GetPage(typeof(T)) as T;

    public FrameworkElement GetPage(Type pageType) {
        if (typeof(FrameworkElement).IsAssignableFrom(pageType)) {
            return (FrameworkElement)_provider.GetRequiredService(pageType);
        }

        throw new InvalidOperationException("Page must be a WPF control.");
    }
}