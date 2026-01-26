using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Wpf.Ui;

namespace Nameless.InfoPhoenix.Client;

/// <summary>
///     <see cref="IServiceCollection"/> extension methods.
/// </summary>
public static class ServiceCollectionExtensions {
    extension(IServiceCollection self) {
        public IServiceCollection RegisterTimeProvider() {
            self.TryAddSingleton(TimeProvider.System);

            return self;
        }

        public IServiceCollection RegisterContentDialogService() {
            self.TryAddSingleton<IContentDialogService, ContentDialogService>();

            return self;
        }

        public IServiceCollection RegisterLogging() {
            self.AddLogging(configure => {
                configure.ClearProviders();
                configure.AddNLog();
            });

            return self;
        }
    }
}
