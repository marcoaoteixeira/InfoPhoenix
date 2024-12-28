using System.Text;
using Nameless.InfoPhoenix.Office.Documents;
using MSWord_Document = Microsoft.Office.Interop.Word.Document;
using MSWord_DocumentEvents_Event = Microsoft.Office.Interop.Word.DocumentEvents_Event;
using MSWord_HeaderFooter = Microsoft.Office.Interop.Word.HeaderFooter;
using MSWord_Range = Microsoft.Office.Interop.Word.Range;
using MSWord_Section = Microsoft.Office.Interop.Word.Section;
using MSWord_WdSaveFormat = Microsoft.Office.Interop.Word.WdSaveFormat;

namespace Nameless.InfoPhoenix.Office;

public sealed class WordDocument : IWordDocument {
    private object _missing = Type.Missing;
    private MSWord_Document? _document;
    private bool _disposed;

    public WordDocumentStatus Status { get; private set; }

    public WordDocument(MSWord_Document document) {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        Initialize();
    }

    ~WordDocument() {
        Dispose(disposing: false);
    }

    public string GetContent(bool formatted) {
        BlockAccessAfterDispose();

        var sb = new StringBuilder();

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

        static string ExtractText(MSWord_Range range, bool formatted) {
            return formatted ? range.FormattedText.Text : range.Text;
        }
    }

    public void SaveAs(string filePath, DocumentType type) {
        BlockAccessAfterDispose();

        var currentFormat = (object)Convert(type);
        object? currentOutputFilePath = filePath;

        GetDocument().SaveAs2(FileName: ref currentOutputFilePath,
                              FileFormat: ref currentFormat,
                              LockComments: ref _missing,
                              Password: ref _missing,
                              AddToRecentFiles: ref _missing,
                              WritePassword: ref _missing,
                              ReadOnlyRecommended: ref _missing,
                              EmbedTrueTypeFonts: ref _missing,
                              SaveNativePictureFormat: ref _missing,
                              SaveFormsData: ref _missing,
                              SaveAsAOCELetter: ref _missing,
                              Encoding: ref _missing,
                              InsertLineBreaks: ref _missing,
                              AllowSubstitutions: ref _missing,
                              LineEnding: ref _missing,
                              AddBiDiMarks: ref _missing,
                              CompatibilityMode: ref _missing);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Initialize() {
        if (_document is not MSWord_DocumentEvents_Event evt) {
            return;
        }

        evt.Close += OnClose;
        Status = WordDocumentStatus.Opened;
    }

    private void OnClose() {
        if (_document is not MSWord_DocumentEvents_Event evt) {
            return;
        }

        evt.Close -= OnClose;
        Status = WordDocumentStatus.Closed;
    }

    private void Dispose(bool disposing) {
        if (_disposed) { return; }

        if (disposing) {
            _document?.Close(SaveChanges: ref _missing,
                             OriginalFormat: ref _missing,
                             RouteDocument: ref _missing);
        }

        _document = null;
        _disposed = true;
    }

    private void BlockAccessAfterDispose()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    private MSWord_Document GetDocument()
        => _document ?? throw new ArgumentNullException(nameof(_document));

    private static MSWord_WdSaveFormat Convert(DocumentType type)
        => type switch {
            DocumentType.RichTextFormat => MSWord_WdSaveFormat.wdFormatRTF,
            DocumentType.Word => MSWord_WdSaveFormat.wdFormatDocument,
            DocumentType.XPS => MSWord_WdSaveFormat.wdFormatXPS,
            _ => throw new InvalidOperationException("Invalid document format type.")
        };
}