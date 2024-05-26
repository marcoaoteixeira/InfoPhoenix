using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Nameless.InfoPhoenix.Text.Impl {
    public sealed class PDFDocumentReader : IDocumentReader {
        #region IDocumentReader Members

        public string[] Extensions => [".pdf"];

        public string GetContent(string filePath) {
            var sb = new StringBuilder();
            using var document = PdfDocument.Open(filePath);

            foreach (var page in document.GetPages()) {
                var text = ContentOrderTextExtractor.GetText(page);

                sb.Append(text);
            }

            return sb.ToString();
        }

        #endregion
    }
}
