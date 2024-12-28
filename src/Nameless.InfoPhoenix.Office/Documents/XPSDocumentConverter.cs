using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Documents;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Office.Documents;

public sealed class XPSDocumentConverter : IDocumentConverter {
    private readonly IFileSystem _fileSystem;
    private readonly IWordApplication _wordApplication;
    private readonly ILogger<XPSDocumentConverter> _logger;

    private readonly string[] _validExtensions = [".doc", ".docx", ".rtf", ".txt"];

    public XPSDocumentConverter(IWordApplication wordApplication,
                                IFileSystem fileSystem,
                                ILogger<XPSDocumentConverter> logger) {
        _wordApplication = Prevent.Argument.Null(wordApplication);
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _logger = Prevent.Argument.Null(logger);
    }

    public Result<byte[]> Convert(string filePath) {
        Prevent.Argument.NullOrWhiteSpace(filePath);

        if (!_fileSystem.File.Exists(filePath) ||
            !_validExtensions.Contains(_fileSystem.Path.GetExtension(filePath))) {
            return Array.Empty<byte>();
        }

        var result = Array.Empty<byte>();
        try {
            if (TrySaveAsXpsDocument(filePath, out var xspFilePath)) {
                result = _fileSystem.File.ReadAllBytes(xspFilePath);
                _fileSystem.File.Delete(xspFilePath);
            }
        }
        catch (Exception ex) { return Error.Failure(ex.Message); }
        return result;
    }

    private bool TrySaveAsXpsDocument(string filePath, [NotNullWhen(returnValue: true)] out string? xspFilePath) {
        xspFilePath = null;
        try {
            xspFilePath = _fileSystem.Path.GetTempFileName();

            using var document = _wordApplication.Open(filePath);
            document.SaveAs(xspFilePath, DocumentType.XPS);

            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "An error occurred while converting file to XPS. FilePath: {FilePath}", filePath);

            return false;
        }
    }
}