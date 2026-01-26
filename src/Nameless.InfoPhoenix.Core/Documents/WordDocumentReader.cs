using System.IO;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Office;

namespace Nameless.InfoPhoenix.Documents;

public class WordDocumentReader : IDocumentReader {
    private readonly IWordApplication _wordApplication;
    private readonly ILogger<WordDocumentReader> _logger;

    public WordDocumentReader(IWordApplication wordApplication, ILogger<WordDocumentReader> logger) {
        _wordApplication = wordApplication;
        _logger = logger;
    }
    public bool CanRead(string filePath) {
        return string.Equals(Path.GetExtension(filePath), Constants.WellKnownDocuments.Doc, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Path.GetExtension(filePath), Constants.WellKnownDocuments.Docx, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Path.GetExtension(filePath), Constants.WellKnownDocuments.Rtf, StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> GetContentAsync(string filePath, CancellationToken cancellationToken) {
        try {
            using var document = _wordApplication.Open(filePath);

            var result = document.GetContent(formatted: false);

            return Task.FromResult(result);
        }
        catch (Exception ex) { _logger.GetContentFailure(filePath, ex); }

        return Task.FromResult(string.Empty);
    }
}