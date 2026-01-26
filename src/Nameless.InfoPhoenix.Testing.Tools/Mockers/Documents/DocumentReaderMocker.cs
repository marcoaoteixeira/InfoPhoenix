using Moq;
using Nameless.InfoPhoenix.Documents;
using Nameless.Testing.Tools.Mockers;

namespace Nameless.InfoPhoenix.Testing.Tools.Mockers.Documents;

public class DocumentReaderMocker : Mocker<IDocumentReader> {
    public DocumentReaderMocker WithCanRead(bool returnValue) {
        MockInstance
            .Setup(mock => mock.CanRead(It.IsAny<string>()))
            .Returns(returnValue);

        return this;
    }

    public DocumentReaderMocker WithGetContentAsync(string returnValue) {
        MockInstance
            .Setup(mock => mock.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnValue);

        return this;
    }
}
