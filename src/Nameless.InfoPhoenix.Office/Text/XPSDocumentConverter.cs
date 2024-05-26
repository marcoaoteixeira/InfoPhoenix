using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Text;

namespace Nameless.InfoPhoenix.Office.Text {
    public sealed class XPSDocumentConverter : IDocumentConverter {
        #region Private Read-Only Fields

        private readonly IWordApplication _wordApplication;
        private readonly ILogger _logger;

        private readonly string[] _validExtensions = [".doc", ".docx", ".rtf", ".txt"];

        #endregion

        #region Public Constructors

        public XPSDocumentConverter(IWordApplication wordApplication, ILogger<XPSDocumentConverter> logger) {
            _wordApplication = Guard.Against.Null(wordApplication, nameof(wordApplication));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        #endregion

        #region Private Methods

        private bool TrySaveAsXpsDocument(string filePath, [NotNullWhen(returnValue: true)]out string? xspFilePath) {
            xspFilePath = null;
            try {
                xspFilePath = Path.GetTempFileName();
                using var document = _wordApplication.Open(filePath);

                document.SaveAs(xspFilePath, DocumentType.XPS);
                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error while converting file to XPS. FilePath: {FilePath}", filePath);
                return false;
            }
        }

        #endregion

        #region XPSDocumentConverter Members

        public byte[] Convert(string filePath) {
            Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));

            if (!File.Exists(filePath) ||
                !_validExtensions.Contains(Path.GetExtension(filePath))) { return []; }

            return TrySaveAsXpsDocument(filePath, out var xspFilePath)
                ? File.ReadAllBytes(xspFilePath)
                : [];
        } 

        #endregion
    }
}
