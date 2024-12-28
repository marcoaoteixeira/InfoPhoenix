using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Application.Windows;

public sealed class WindowFactory : IWindowFactory {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WindowFactory> _logger;

    public WindowFactory(IServiceProvider serviceProvider) {
        _serviceProvider = Prevent.Argument.Null(serviceProvider);
        _logger = _serviceProvider.GetLogger<WindowFactory>();
    }

    public bool TryCreate<TWindow>(object? owner, [NotNullWhen(returnValue: true)] out TWindow? window)
        where TWindow : IWindow {
        window = _serviceProvider.GetService<TWindow>();

        if (window is null) {
            _logger.LogWarning("O serviço requisitado ({Window}) não foi registrado no contêiner.", typeof(TWindow).Name);

            return false;
        }

        window.SetOwner(owner);

        return true;
    }
}