using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Nameless.Test.Utils.Mockers;
public class FileInfoMocker : MockerBase<IFileInfo> {
    public FileInfoMocker WithExists(bool exists) {
        InnerMock.Setup(mock => mock.Exists)
                 .Returns(exists);

        return this;
    }

    public FileInfoMocker WithLength(long length) {
        InnerMock.Setup(mock => mock.Length)
                 .Returns(length);

        return this;
    }

    public FileInfoMocker WithPhysicalPath(string physicalPath) {
        InnerMock.Setup(mock => mock.PhysicalPath)
                 .Returns(physicalPath);

        return this;
    }

    public FileInfoMocker WithName(string name) {
        InnerMock.Setup(mock => mock.Name)
                 .Returns(name);

        return this;
    }

    public FileInfoMocker WithLastModified(DateTimeOffset lastModified) {
        InnerMock.Setup(mock => mock.LastModified)
                 .Returns(lastModified);

        return this;
    }

    public FileInfoMocker WithIsDirectory(bool isDirectory) {
        InnerMock.Setup(mock => mock.IsDirectory)
                 .Returns(isDirectory);

        return this;
    }

    public FileInfoMocker WithCreateReadStream(Stream readStream) {
        InnerMock.Setup(mock => mock.CreateReadStream())
                 .Returns(readStream);

        return this;
    }

    public FileInfoMocker WithCreateReadStream(string content) {
        InnerMock.Setup(mock => mock.CreateReadStream())
                 .Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        return this;
    }
}
