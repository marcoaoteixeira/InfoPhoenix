using Nameless.FileSystem;
using Nameless.InfoPhoenix.Documents;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Office.Documents;

public sealed class TextDocumentReader : IDocumentReader {
    private readonly IFileSystem _fileSystem;

    public TextDocumentReader(IFileSystem fileSystem) {
        _fileSystem = Prevent.Argument.Null(fileSystem);
    }

    public bool CanRead(string extension)
        => string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase);

    public Result<string> GetContent(string filePath) {
        try {
            return _fileSystem.File.Exists(filePath)
                ? _fileSystem.File.ReadAllText(filePath)
                : string.Empty;
        } catch (Exception ex) { return Error.Failure(ex.Message); }
    }
}