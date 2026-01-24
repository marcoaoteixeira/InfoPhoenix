using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nameless.WPF.DependencyInjection;

namespace Nameless.InfoPhoenix.Documents;

public static class ServiceCollectionExtensions {
    extension(IServiceCollection self) {
        public IServiceCollection RegisterDocumentServices(Action<DocumentServicesOptions>? configure = null) {
            var innerConfigure = configure ?? (_ => { });
            var options = new DocumentServicesOptions();

            innerConfigure(options);

            self.RegisterDocumentReaders(options);
            self.RegisterDocumentConverters(options);
            self.TryAddSingleton<IDocumentService, DocumentService>();

            return self;
        }

        private void RegisterDocumentReaders(DocumentServicesOptions options) {
            var service = typeof(IDocumentReader);
            var descriptors = options.Assemblies
                                     .GetImplementations(service)
                                     .Where(type => !type.IsGenericTypeDefinition)
                                     .Select(type => type.CreateServiceDescriptor(service));

            self.TryAddEnumerable(descriptors);
        }

        private void RegisterDocumentConverters(DocumentServicesOptions options) {
            var service = typeof(IDocumentConverter);
            var descriptors = options.Assemblies
                                     .GetImplementations(service)
                                     .Where(type => !type.IsGenericTypeDefinition)
                                     .Select(type => type.CreateServiceDescriptor(service));

            self.TryAddEnumerable(descriptors);
        }
    }
}

public class DocumentServicesOptions {
    public Assembly[] Assemblies { get; set; } = [];
}