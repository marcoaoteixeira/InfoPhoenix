using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Nameless.InfoPhoenix.Documents;

public class PdfDocumentReader : IDocumentReader {
    private readonly ILogger<PdfDocumentReader> _logger;

    public PdfDocumentReader(ILogger<PdfDocumentReader> logger) {
        _logger = logger;
    }
    public bool CanRead(string filePath) {
        return string.Equals(Path.GetExtension(filePath), Constants.WellKnownDocuments.Pdf, StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> GetContentAsync(string filePath, CancellationToken cancellationToken) {
        var sb = new StringBuilder();

        try {
            using var document = PdfDocument.Open(filePath);

            foreach (var page in document.GetPages()) {
                var content = ContentOrderTextExtractor.GetText(page);

                sb.Append(content);
            }
        }
        catch (Exception ex) { _logger.GetContentFailure(filePath, ex); }

        return Task.FromResult(sb.ToString());
    }
}