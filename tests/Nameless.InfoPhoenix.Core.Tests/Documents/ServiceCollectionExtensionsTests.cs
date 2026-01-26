using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Office;
using Nameless.Testing.Tools;
using Nameless.Testing.Tools.Attributes;

namespace Nameless.InfoPhoenix.Documents;

[UnitTest]
public class ServiceCollectionExtensionsTests {
    [Fact]
    public void WhenRegisteringServices_ThenResolveServices() {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<PdfDocumentReader>>(NullLogger<PdfDocumentReader>.Instance);
        services.AddSingleton<ILogger<PlainTextDocumentReader>>(NullLogger<PlainTextDocumentReader>.Instance);
        services.AddSingleton<ILogger<WordDocumentReader>>(NullLogger<WordDocumentReader>.Instance);
        services.AddSingleton<ILogger<XpsDocumentReader>>(NullLogger<XpsDocumentReader>.Instance);
        services.AddSingleton(Quick.Mock<IWordApplication>());
        services.RegisterDocumentServices(opts => {
            opts.Assemblies = [typeof(AssemblyMarker).Assembly];
        });
        var provider = services.BuildServiceProvider();
        var expectedDocumentReaders = new[] {
            typeof(PdfDocumentReader),
            typeof(PlainTextDocumentReader),
            typeof(WordDocumentReader),
            typeof(XpsDocumentReader),
        };

        // act
        var documentReaderManager = provider.GetService<IDocumentService>();
        var documentReaders = provider.GetServices<IDocumentReader>()
                                      .Select(item => item.GetType())
                                      .ToArray();

        // assert
        Assert.Multiple(() => {
            Assert.IsType<DocumentService>(documentReaderManager);
            Assert.Equivalent(expectedDocumentReaders, documentReaders);
        });
    }
}
