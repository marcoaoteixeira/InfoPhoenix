using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Nameless.InfoPhoenix.Bootstrapping;
using Nameless.InfoPhoenix.Bootstrapping.Impl;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Configuration.Impl;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Infrastructure.Impl;
using Nameless.InfoPhoenix.Text;
using Nameless.InfoPhoenix.Text.Impl;
using Nameless.Infrastructure;

namespace Nameless.InfoPhoenix {
    public static class ServiceCollectionExtension {
        #region Public Static Methods

        public static IServiceCollection RegisterBootstrap(this IServiceCollection self, Assembly[] supportAssemblies) {
            var types = supportAssemblies
                        .SelectMany(assembly => assembly.GetExportedTypes())
                        .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                                       typeof(IStep).IsAssignableFrom(type))
                        .ToArray();

            foreach (var type in types) {
                self.AddTransient(typeof(IStep), type);
            }

            return self.AddTransient<IBootstrap>(provider => {
                var steps = provider.GetRequiredService<IEnumerable<IStep>>();
                var logger = provider.GetLogger<Bootstrap>();

                return new Bootstrap(steps, logger);
            });
        }

        public static IServiceCollection RegisterAppConfigurationContext(this IServiceCollection self)
            => self.AddSingleton<IAppConfigurationContext, AppConfigurationContext>();

        public static IServiceCollection RegisterFileProvider(this IServiceCollection self)
            => self
                .AddSingleton<IFileProvider>(provider => {
                    var applicationContext = provider.GetRequiredService<IApplicationContext>();
                    var root = applicationContext.ApplicationDataFolderPath;

                    return new PhysicalFileProvider(root);
                });

        public static IServiceCollection RegisterDocumentReaderProvider(this IServiceCollection self)
            => self.AddSingleton<IDocumentReaderProvider, DocumentReaderProvider>();

        public static IServiceCollection RegisterTextDocumentReader(this IServiceCollection self)
            => self.AddSingleton<IDocumentReader, TextDocumentReader>();

        public static IServiceCollection RegisterPDFDocumentReader(this IServiceCollection self)
            => self.AddSingleton<IDocumentReader, PDFDocumentReader>();

        public static IServiceCollection RegisterMediatR(this IServiceCollection self, Assembly[] supportAssemblies)
            => self.AddMediatR(setup => setup.RegisterServicesFromAssemblies(supportAssemblies));

        public static IServiceCollection RegisterPerformanceWatcher(this IServiceCollection self)
            => self.AddSingleton<IPerformanceReporter, PerformanceReporter>();

        public static IServiceCollection RegisterPubSubService(this IServiceCollection self)
            => self.AddSingleton<IPubSubService>(new PubSubService(new WeakReferenceMessenger()));

        #endregion
    }
}
