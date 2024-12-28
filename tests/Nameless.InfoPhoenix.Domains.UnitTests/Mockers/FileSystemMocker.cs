using Moq;
using Nameless.FileSystem;
using Nameless.Test.Utils;

namespace Nameless.InfoPhoenix.Domains.Mockers;

public class FileSystemMocker : MockerBase<IFileSystem> {
    public FileSystemMocker WithDirectory(IDirectoryService directoryService) {
        InnerMock.Setup(mock => mock.Directory)
                 .Returns(directoryService);

        return this;
    }

    public FileSystemMocker WithFile(IFileService fileService) {
        InnerMock.Setup(mock => mock.File)
                 .Returns(fileService);

        return this;
    }

    public FileSystemMocker WithPath(IPathService pathService) {
        InnerMock.Setup(mock => mock.Path)
                 .Returns(pathService);

        return this;
    }
}

public class FileServiceMocker : MockerBase<IFileService> {
    public FileServiceMocker WithOpen(string content) {
        InnerMock.Setup(mock => mock.Open(It.IsAny<string>(),
                                          It.IsAny<FileMode>(),
                                          It.IsAny<FileAccess>(),
                                          It.IsAny<FileShare>()))
                 .Returns(content.ToMemoryStream());

        return this;
    }

    public FileServiceMocker WithCreate() {
        InnerMock.Setup(mock => mock.Create(It.IsAny<string>()))
                 .Returns(new MemoryStream());

        return this;
    }
}

public class DirectoryServiceMocker : MockerBase<IDirectoryService> {

}

public class PathServiceMocker : MockerBase<IPathService> {

}