using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nameless.Infrastructure;
using Nameless.WPF.DependencyInjection;

namespace Nameless.InfoPhoenix.Data;

/// <summary>
///     <see cref="IServiceCollection"/> extension methods.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <param name="self">
    ///     The current <see cref="IServiceCollection"/>.
    /// </param>
    extension(IServiceCollection self) {
        /// <summary>
        ///     Registers the application database context.
        /// </summary>
        /// <param name="configure">
        ///     The configuration action.
        /// </param>
        /// <returns>
        ///     The current <see cref="IServiceCollection"/> so other actions
        ///     can be chained.
        /// </returns>
        public IServiceCollection RegisterAppDbContext(Action<AppDbContextOptions>? configure = null) {
            var innerConfigure = configure ?? (_ => { });
            var options = new AppDbContextOptions();

            innerConfigure(options);

            self.RegisterInterceptors(options);

            self.AddDbContext<AppDbContext>((provider, builder) => {
                var applicationContext = provider.GetRequiredService<IApplicationContext>();
                var databaseFilePath = Path.Combine(
                    applicationContext.DataDirectoryPath,
                    Constants.Database.DirectoryName,
                    Constants.Database.Name
                );
                var connectionString = string.Format(Constants.Database.ConnStrPattern, databaseFilePath);
                var interceptors = provider.GetServices<IInterceptor>();

                builder.UseSqlite(connectionString)
                       .AddInterceptors(interceptors)
                       .UseSeeding(options.Seeding)
                       .UseAsyncSeeding(options.SeedingAsync);
            });

            return self;
        }

        private void RegisterInterceptors(AppDbContextOptions options) {
            var service = typeof(IInterceptor);
            var descriptors = options.Interceptors
                                     .Where(type => !type.IsGenericTypeDefinition)
                                     .Select(implementation => CreateServiceDescriptor(service, implementation));

            self.TryAddEnumerable(descriptors);

            return;

            static ServiceDescriptor CreateServiceDescriptor(Type service, Type implementation) {
                var lifetime = ServiceLifetimeAttribute.GetLifetime(implementation);

                return new ServiceDescriptor(service, implementation, lifetime);
            }
        }
    }
}
