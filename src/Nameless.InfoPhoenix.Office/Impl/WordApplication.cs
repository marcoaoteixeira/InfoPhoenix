using MSWord_Application = Microsoft.Office.Interop.Word.Application;
using MSWord_Document = Microsoft.Office.Interop.Word.Document;
using MSWord_WdWindowState = Microsoft.Office.Interop.Word.WdWindowState;

namespace Nameless.InfoPhoenix.Office.Impl {
    public sealed class WordApplication : IWordApplication {
        #region Private Fields

        private MSWord_Application? _application;
        private bool _disposed;

        private object Missing = Type.Missing;
        private object SetDocumentVisible = false;

        #endregion

        #region Destructor

        ~WordApplication() {
            Dispose(disposing: false);
        }

        #endregion

        #region Private Methods

        private MSWord_Application GetApplication() {
            _application ??= new MSWord_Application {
                Visible = false,
                WindowState = MSWord_WdWindowState.wdWindowStateMinimize,
            };
            return _application;
        }

        private void ReleaseDocuments() {
            foreach (MSWord_Document document in GetApplication().Documents) {
                document.Close(
                    SaveChanges: ref Missing,
                    OriginalFormat: ref Missing,
                    RouteDocument: ref Missing
                );
            }
        }

        private void Quit()
            => _application?.Quit(
                SaveChanges: ref Missing,
                OriginalFormat: ref Missing,
                RouteDocument: ref Missing
            );

        private void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                ReleaseDocuments();
                Quit();
            }

            _application = null;
            _disposed = true;
        }

        private void BlockAccessAfterDispose() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(WordApplication));
            }
        }

        #endregion

        #region IWordApplicationWrapper Members

        public IWordDocument Open(string filePath) {
            BlockAccessAfterDispose();

            if (string.IsNullOrWhiteSpace(filePath)) {
                throw new ArgumentException("Argument cannot be null, empty or white spaces.", nameof(filePath));
            }

            var currentFilePath = (object)filePath;
            var document = GetApplication()
                           .Documents
                           .Open(
                               FileName: ref currentFilePath,
                               ConfirmConversions: ref Missing,
                               ReadOnly: ref Missing,
                               AddToRecentFiles: ref Missing,
                               PasswordDocument: ref Missing,
                               PasswordTemplate: ref Missing,
                               Revert: ref Missing,
                               WritePasswordDocument: ref Missing,
                               WritePasswordTemplate: ref Missing,
                               Format: ref Missing,
                               Encoding: ref Missing,
                               Visible: ref SetDocumentVisible,
                               OpenAndRepair: ref Missing,
                               DocumentDirection: ref Missing,
                               NoEncodingDialog: ref Missing,
                               XMLTransform: ref Missing
                           );

            return new WordDocument(document);
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
