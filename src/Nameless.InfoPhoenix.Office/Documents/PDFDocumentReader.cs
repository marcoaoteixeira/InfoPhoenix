using System.Text;
using Nameless.InfoPhoenix.Documents;
using Nameless.Result;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Nameless.InfoPhoenix.Office.Documents;

public sealed class PDFDocumentReader : IDocumentReader {
    public bool CanRead(string extension)
        => string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);

    public Result<string> GetContent(string filePath) {
        var sb = new StringBuilder();

        try {
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages()) {
                var text = ContentOrderTextExtractor.GetText(page);

                sb.Append(text);
            }
        } catch (Exception ex) { return Error.Failure(ex.Message); }

        return sb.ToString();
    }
}