using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Office.Documents;

namespace Nameless.InfoPhoenix.Office;

public static class ServiceCollectionExtension {
    public static IServiceCollection RegisterDocumentServices(this IServiceCollection self, Assembly[] supportAssemblies) {
        var documentReaders = supportAssemblies.SelectMany(assembly => assembly.SearchForImplementations<IDocumentReader>())
                                               .ToArray();

        foreach (var documentReader in documentReaders) {
            self.AddSingleton(typeof(IDocumentReader), documentReader);
        }

        return self.AddSingleton<IWordApplication, WordApplication>()
                   .AddSingleton<IDocumentReaderManager, DocumentReaderManager>()
                   .AddSingleton<IDocumentConverter, XPSDocumentConverter>();
    }
}