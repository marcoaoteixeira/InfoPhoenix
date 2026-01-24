using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Nameless.Helpers;
using Nameless.InfoPhoenix.Office;
using Nameless.IO.FileSystem;

namespace Nameless.InfoPhoenix.Documents;

public class XpsDocumentConverter : IDocumentConverter {
    private readonly IFileSystem _fileSystem;
    private readonly IWordApplication _wordApplication;
    private readonly ILogger<XpsDocumentConverter> _logger;

    public DocumentType OutputType => DocumentType.XPS;

    public XpsDocumentConverter(IFileSystem fileSystem, IWordApplication wordApplication, ILogger<XpsDocumentConverter> logger) {
        _fileSystem = fileSystem;
        _wordApplication = wordApplication;
        _logger = logger;
    }

    public bool CanConvert(DocumentType type) {
        return type switch {
            DocumentType.DOCX => true,
            DocumentType.RTF => true,
            DocumentType.TXT => true,
            _ => false
        };
    }

    public Task<string> ConvertAsync(string filePath, CancellationToken cancellationToken) {
        EnsureTemporaryDirectoryExistence();

        var relativeXpsFilePath = Path.Combine(
            Constants.Application.Temporary.DirectoryName,
            string.Concat(GetTemporaryFileName(filePath), ".xps")
        );
        var xpsFile = _fileSystem.GetFile(relativeXpsFilePath);

        if (xpsFile.Exists) {
            return Task.FromResult(xpsFile.Path);
        }

        try {
            using var document = _wordApplication.Open(filePath);

            document.SaveAs(xpsFile.Path, DocumentType.XPS);
        }
        catch (Exception ex) {
            _logger.ConvertFailure(filePath, ex);

            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(xpsFile.Path);
    }

    private static string GetTemporaryFileName(string filePath) {
        filePath = PathHelper.Normalize(filePath);

        Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetByteCount(filePath)];
        Encoding.UTF8.GetBytes(filePath, buffer);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(buffer, hash);

        return Convert.ToHexStringLower(hash);
    }

    private void EnsureTemporaryDirectoryExistence() {
        _fileSystem.GetDirectory(Constants.Application.Temporary.DirectoryName)
                   .Create();
    }
}