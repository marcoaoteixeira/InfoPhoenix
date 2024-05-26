using System.IO;
using System.Text;
using System.Windows.Xps.Packaging;
using Nameless.InfoPhoenix.Text;

namespace Nameless.InfoPhoenix.UI.Text.Impl {
    public sealed class XPSDocumentReader : IDocumentReader {
        #region IDocumentReader Members

        public string[] Extensions => [".xps"];

        public string GetContent(string filePath) {
            const string UNICODE_STR = "UnicodeString";
            const string GLYPHS = "Glyphs";

            var sb = new StringBuilder();

            using var xpsDocument = new XpsDocument(filePath, FileAccess.Read);
            var reader = xpsDocument.FixedDocumentSequenceReader;
            if (reader is null) { return string.Empty; }

            foreach (var fixedDocument in reader.FixedDocuments) {
                foreach (var fixedPage in fixedDocument.FixedPages) {
                    var pageReader = fixedPage.XmlReader;
                    if (pageReader is null) { continue; }

                    while (pageReader.Read()) {
                        if (pageReader.Name != GLYPHS || !pageReader.HasAttributes) { continue; }

                        var pageContent = pageReader.GetAttribute(UNICODE_STR);
                        if (pageContent is null) {
                            continue;
                        }
                        sb.AppendLine(pageContent);
                    }
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
