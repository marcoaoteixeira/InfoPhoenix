using Microsoft.Extensions.Logging;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Documents;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Office.Documents;

public sealed class DocumentReaderManager : IDocumentReaderManager {
    private readonly IDocumentReader[] _documentReaders;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DocumentReaderManager> _logger;

    public DocumentReaderManager(IEnumerable<IDocumentReader> documentReaders,
                                 IFileSystem fileSystem,
                                 ILogger<DocumentReaderManager> logger) {
        _documentReaders = Prevent.Argument.Null(documentReaders).ToArray();
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _logger = Prevent.Argument.Null(logger);
    }

    public Result<string> GetDocumentContent(string filePath) {
        var extension = _fileSystem.Path.GetExtension(filePath) ?? string.Empty;
        var documentReader = GetDocumentReader(extension);

        return documentReader is not null
            ? documentReader.GetContent(filePath)
            : Error.Conflict("Não foi possível localizar um leitor para o documento.");
    }

    private IDocumentReader? GetDocumentReader(string extension) {
        var result = _documentReaders.FirstOrDefault(Match);

        _logger.OnCondition(result is null)
               .LogInformation("Could not find document reader for extension '{Extension}'", extension);

        return result;

        bool Match(IDocumentReader documentReader) {
            return documentReader.CanRead(extension);
        }
    }
}