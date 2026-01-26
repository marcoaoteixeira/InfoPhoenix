using Moq;
using Nameless.InfoPhoenix.Documents;
using Nameless.Testing.Tools.Mockers;

namespace Nameless.InfoPhoenix.Testing.Tools.Mockers.Documents;

public class DocumentReaderManagerMocker : Mocker<IDocumentService> {

    public DocumentReaderManagerMocker WithGetContentAsync(string returnValue) {
        MockInstance
            .Setup(mock => mock.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(returnValue));

        return this;
    }
}
