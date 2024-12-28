using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Documents;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Office.Documents;

public sealed class WordDocumentReader : IDocumentReader {
    private static readonly string[] Extensions = [".doc", ".docx", ".rtf"];

    private readonly IWordApplication _wordApplication;
    private readonly ILogger<WordDocumentReader> _logger;

    public bool CanRead(string extension)
        => Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    public WordDocumentReader(IWordApplication wordApplication, ILogger<WordDocumentReader> logger) {
        _wordApplication = Prevent.Argument.Null(wordApplication);
        _logger = Prevent.Argument.Null(logger);
    }

    public Result<string> GetContent(string filePath) {
        try {
            using var doc = _wordApplication.Open(filePath);

            return doc.GetContent(formatted: false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Couldn't read file {FilePath}", filePath);

            return Error.Failure(ex.Message);
        }
    }
}