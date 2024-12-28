using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Nameless.Test.Utils.Mockers;

public class FileProviderMocker : MockerBase<IFileProvider> {
    public FileProviderMocker WithFileInfo(IFileInfo fileInfo) {
        InnerMock.Setup(mock => mock.GetFileInfo(It.IsAny<string>()))
                 .Returns(fileInfo);

        return this;
    }

    public FileProviderMocker WithFileInfo(IFileInfo fileInfo, string subPath) {
        InnerMock.Setup(mock => mock.GetFileInfo(subPath))
                 .Returns(fileInfo);

        return this;
    }

    public FileProviderMocker WithWatch(IChangeToken changeToken) {
        InnerMock.Setup(mock => mock.Watch(It.IsAny<string>()))
                 .Returns(changeToken);

        return this;
    }

    public FileProviderMocker WithWatch(IChangeToken changeToken, string filter) {
        InnerMock.Setup(mock => mock.Watch(filter))
                 .Returns(changeToken);

        return this;
    }
}
