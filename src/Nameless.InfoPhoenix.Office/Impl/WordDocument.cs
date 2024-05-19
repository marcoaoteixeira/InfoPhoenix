using System.Text;
using MSWord_Document = Microsoft.Office.Interop.Word.Document;
using MSWord_DocumentEvents_Event = Microsoft.Office.Interop.Word.DocumentEvents_Event;
using MSWord_HeaderFooter = Microsoft.Office.Interop.Word.HeaderFooter;
using MSWord_Range = Microsoft.Office.Interop.Word.Range;
using MSWord_Section = Microsoft.Office.Interop.Word.Section;
using MSWord_WdSaveFormat = Microsoft.Office.Interop.Word.WdSaveFormat;

namespace Nameless.InfoPhoenix.Office.Impl {
    public sealed class WordDocument : IWordDocument {
        #region Private Fields

        private object Missing = Type.Missing;
        private MSWord_Document? _document;
        private bool _disposed;

        #endregion

        #region Public Constructors

        public WordDocument(MSWord_Document document) {
            _document = document ?? throw new ArgumentNullException(nameof(document));

            Initialize();
        }

        #endregion

        #region Destructor

        ~WordDocument() {
            Dispose(disposing: false);
        }

        #endregion

        #region Private Methods

        private void Initialize() {
            if (_document is MSWord_DocumentEvents_Event evt) {
                evt.Close += OnClose;
                Status = WordDocumentStatus.Opened;
            }
        }

        private void OnClose() {
            if (_document is MSWord_DocumentEvents_Event evt) {
                evt.Close -= OnClose;
                Status = WordDocumentStatus.Closed;
            }
        }

        private void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                _document?.Close(
                    SaveChanges: ref Missing,
                    OriginalFormat: ref Missing,
                    RouteDocument: ref Missing
                );
            }

            _document = null;
            _disposed = true;
        }

        private void BlockAccessAfterDispose() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(WordDocument));
            }
        }

        private MSWord_Document GetDocument()
            => _document ?? throw new ArgumentNullException(nameof(_document));

        private static MSWord_WdSaveFormat ParseDocumentType(DocumentType type)
            => type switch {
                DocumentType.RichTextFormat => MSWord_WdSaveFormat.wdFormatRTF,
                DocumentType.Word => MSWord_WdSaveFormat.wdFormatDocument,
                _ => throw new InvalidOperationException("Invalid document format type.")
            };

        #endregion

        #region IWordDocument Members

        public WordDocumentStatus Status { get; private set; }

        public string GetContent(bool formatted) {
            BlockAccessAfterDispose();

            var sb = new StringBuilder();

            static string ExtractText(MSWord_Range range, bool formatted) {
                return formatted ? range.FormattedText.Text : range.Text;
            }

            // Read header
            foreach (MSWord_Section section in GetDocument().Sections) {
                foreach (MSWord_HeaderFooter header in section.Headers) {
                    sb.AppendLine(ExtractText(header.Range, formatted));
                }
            }

            // Read content
            sb.AppendLine(ExtractText(GetDocument().Content, formatted));

            // Read footer
            foreach (MSWord_Section section in GetDocument().Sections) {
                foreach (MSWord_HeaderFooter footer in section.Footers) {
                    sb.AppendLine(ExtractText(footer.Range, formatted));
                }
            }

            return sb.ToString();
        }

        public void SaveAs(string filePath, DocumentType type) {
            BlockAccessAfterDispose();

            var currentFormat = (object)ParseDocumentType(type);
            var currentOutputFilePath = (object)filePath;

            GetDocument().SaveAs2(
                FileName: ref currentOutputFilePath,
                FileFormat: ref currentFormat,
                LockComments: ref Missing,
                Password: ref Missing,
                AddToRecentFiles: ref Missing,
                WritePassword: ref Missing,
                ReadOnlyRecommended: ref Missing,
                EmbedTrueTypeFonts: ref Missing,
                SaveNativePictureFormat: ref Missing,
                SaveFormsData: ref Missing,
                SaveAsAOCELetter: ref Missing,
                Encoding: ref Missing,
                InsertLineBreaks: ref Missing,
                AllowSubstitutions: ref Missing,
                LineEnding: ref Missing,
                AddBiDiMarks: ref Missing,
                CompatibilityMode: ref Missing
            );
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
