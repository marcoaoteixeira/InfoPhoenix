using MSWord_Application = Microsoft.Office.Interop.Word.Application;
using MSWord_Document = Microsoft.Office.Interop.Word.Document;
using MSWord_WdWindowState = Microsoft.Office.Interop.Word.WdWindowState;

namespace Nameless.InfoPhoenix.Office;

public sealed class WordApplication : IWordApplication {
    private MSWord_Application? _application;
    private bool _disposed;

    private object _missing = Type.Missing;
    private object _setDocumentVisible = false;

    ~WordApplication() {
        Dispose(disposing: false);
    }

    public IWordDocument Open(string filePath) {
        BlockAccessAfterDispose();

        Prevent.Argument.NullOrWhiteSpace(filePath);

        object? currentFilePath = filePath;
        var document = GetApplication()
                       .Documents
                       .Open(FileName: ref currentFilePath,
                             ConfirmConversions: ref _missing,
                             ReadOnly: ref _missing,
                             AddToRecentFiles: ref _missing,
                             PasswordDocument: ref _missing,
                             PasswordTemplate: ref _missing,
                             Revert: ref _missing,
                             WritePasswordDocument: ref _missing,
                             WritePasswordTemplate: ref _missing,
                             Format: ref _missing,
                             Encoding: ref _missing,
                             Visible: ref _setDocumentVisible,
                             OpenAndRepair: ref _missing,
                             DocumentDirection: ref _missing,
                             NoEncodingDialog: ref _missing,
                             XMLTransform: ref _missing);

        return new WordDocument(document);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private MSWord_Application GetApplication()
        => _application ??= new MSWord_Application {
            Visible = false,
            WindowState = MSWord_WdWindowState.wdWindowStateMinimize,
        };

    private void ReleaseDocuments() {
        foreach (MSWord_Document document in GetApplication().Documents) {
            document.Close(SaveChanges: ref _missing,
                           OriginalFormat: ref _missing,
                           RouteDocument: ref _missing);
        }
    }

    private void Quit()
        => _application?.Quit(SaveChanges: ref _missing,
                              OriginalFormat: ref _missing,
                              RouteDocument: ref _missing);

    private void Dispose(bool disposing) {
        if (_disposed) { return; }

        if (disposing) {
            ReleaseDocuments();
            Quit();
        }

        _application = null;
        _disposed = true;
    }

    private void BlockAccessAfterDispose()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}