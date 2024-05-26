using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Text;

namespace Nameless.InfoPhoenix.Office.Text {
    public sealed class WordDocumentReader : IDocumentReader {
        #region Private Read-Only Fields

        private readonly IWordApplication _wordApplication;
        private readonly ILogger<WordDocumentReader> _logger;

        #endregion

        #region Public Constructors

        public WordDocumentReader(IWordApplication wordApplication, ILogger<WordDocumentReader> logger) {
            _wordApplication = Guard.Against.Null(wordApplication, nameof(wordApplication));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        #endregion

        #region IDocumentReader Members

        public string[] Extensions => [".doc", ".docx", ".rtf"];

        public string GetContent(string filePath) {
            var result = string.Empty;

            try {
                using var doc = _wordApplication.Open(filePath);
                result = doc.GetContent(formatted: false);
            }
            catch (Exception ex) { _logger.LogError(ex, "Couldn't read file {FilePath}", filePath); }

            return result;
        }

        #endregion
    }
}
