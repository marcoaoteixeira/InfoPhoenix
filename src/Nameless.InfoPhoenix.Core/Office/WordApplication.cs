using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nameless.InfoPhoenix.Office;

public class WordApplication : IWordApplication {
    private const string WORD_PROCESS_NAME = "WINWORD";
    private const string WORD_CAPTION = "WORD_INFOPHOENIX_INSTANCE";

    private readonly Lazy<MSWord_Application> _application;

    private object _missing = Type.Missing;
    private object _setDocumentVisible = false;
    private bool _disposed;

    private MSWord_Application Application => _application.Value;

    /// <summary>
    ///     Initializes a new instance of the
    ///     <see cref="WordApplication"/> class.
    /// </summary>
    public WordApplication() {
        _application = new Lazy<MSWord_Application>(CreateApplication);
    }

    ~WordApplication() {
        Dispose(disposing: false);
    }

    public IWordDocument Open(string filePath) {
        BlockAccessAfterDispose();

        Guard.Against.NullOrWhiteSpace(filePath);

        object? currentFilePath = filePath;

        var document = Application.Documents.Open(
            FileName: ref currentFilePath,
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
            XMLTransform: ref _missing
        );

        return new WordDocument(document);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed) { return; }

        if (disposing) {
            ReleaseDocuments();
            Quit();
        }

        _disposed = true;
    }

    protected void BlockAccessAfterDispose() {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static MSWord_Application CreateApplication() {
        return new MSWord_Application {
            Visible = false,
            WindowState = MSWord_WdWindowState.wdWindowStateMinimize,
            Caption = WORD_CAPTION
        };
    }

    private void ReleaseDocuments() {
        foreach (MSWord_Document document in Application.Documents) {

            document.Close(
                SaveChanges: ref _missing,
                OriginalFormat: ref _missing,
                RouteDocument: ref _missing
            );

            Marshal.ReleaseComObject(document);
        }
    }

    private void Quit() {
        // We need to make sure that we do not leave any hanging instance of
        // Word application running before exit.
        // To ensure that, we're going to get all instances that are marked
        // with the WORD_CAPTION and kill them at the end.

        var processes = Process.GetProcessesByName(WORD_PROCESS_NAME)
                               .Where(item => item.MainWindowTitle == WORD_CAPTION);

        Application.Quit(
            SaveChanges: ref _missing,
            OriginalFormat: ref _missing,
            RouteDocument: ref _missing
        );

        try {
            Marshal.ReleaseComObject(Application);

            foreach (var process in processes) {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(250);
            }
        }
        catch { /* ignore any exception */ }
    }
}