using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nameless.InfoPhoenix.Office.Impl {
    public sealed class OfficeSuite : IOfficeSuite {
        #region Private Read-Only Fields

        private readonly IWordApplication _wordApplication;
        private readonly ILogger _logger;

        #endregion

        #region Public Constructors

        public OfficeSuite(IWordApplication wordApplication)
            : this(wordApplication, NullLogger<OfficeSuite>.Instance) { }

        public OfficeSuite(IWordApplication wordApplication, ILogger<OfficeSuite> logger) {
            _wordApplication = wordApplication ?? throw new ArgumentNullException(nameof(wordApplication));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IOfficeService Members

        public string GetWordDocumentContent(string filePath, bool formatted) {
            try {
                using var doc = _wordApplication.Open(filePath);
                return doc.GetContent(formatted);
            }
            catch (Exception ex) {
                _logger.LogError(
                    exception: ex,
                    message: "Error while trying to read Word document"
                );

                throw;
            }
        }

        #endregion
    }
}
