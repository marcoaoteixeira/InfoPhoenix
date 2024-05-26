using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Xps.Packaging;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Text;
using Nameless.InfoPhoenix.UI.MessageBox;
using Nameless.Infrastructure;
using DomainRoot = Nameless.InfoPhoenix.Domain.Root;

namespace Nameless.InfoPhoenix.Client.Views.Windows {
    public partial class DocumentViewerWindow {
        #region Private Read-Only Fields

        private readonly IMessageBoxService _messageBoxService;
        private readonly IDocumentConverter _documentConverter;
        private readonly ILogger<DocumentViewerWindow> _logger;

        private readonly string _tmpDirectoryPath;

        #endregion

        #region Public Constructors

        public DocumentViewerWindow(IApplicationContext applicationContext, IMessageBoxService messageBoxService, IDocumentConverter documentConverter, ILogger<DocumentViewerWindow> logger) {
            _documentConverter = Guard.Against.Null(documentConverter, nameof(documentConverter));
            _messageBoxService = Guard.Against.Null(messageBoxService, nameof(messageBoxService));
            _logger = Guard.Against.Null(logger, nameof(logger));

            _tmpDirectoryPath = Path.Combine(applicationContext.ApplicationDataFolderPath, "tmp");
            Directory.CreateDirectory(_tmpDirectoryPath);

            InitializeComponent();
        }

        #endregion

        #region Private Static Methods

        private static bool CanDisplay(string filePath) {
            var extension = Path.GetExtension(filePath);

            return extension switch {
                DomainRoot.Files.Extensions.DOC or
                    DomainRoot.Files.Extensions.DOCX or
                    DomainRoot.Files.Extensions.RTF or
                    DomainRoot.Files.Extensions.TXT or
                    DomainRoot.Files.Extensions.XPS => true,
                _ => false
            };
        }

        #endregion

        #region Private Methods

        private XpsDocument OpenXpsDocument(string filePath) {
            var extension = Path.GetExtension(filePath);

            if (extension != DomainRoot.Files.Extensions.XPS) {
                var tmpFilePath = GetTmpDocumentFilePath(filePath);

                if (!File.Exists(tmpFilePath)) {
                    CreateTmpDocument(filePath, tmpFilePath);
                }

                filePath = tmpFilePath;
            }

            return new XpsDocument(filePath, FileAccess.Read, CompressionOption.NotCompressed);
        }

        private string GetTmpDocumentFilePath(string filePath) {
            var buffer = Encoding.UTF8.GetBytes(filePath);
            var hash = SHA256.HashData(buffer);
            var base64 = Convert.ToBase64String(hash);
            var tmpFilePath = Path.Combine(_tmpDirectoryPath, base64);

            return tmpFilePath;
        }

        private void CreateTmpDocument(string sourceFilePath, string destinationFilePath) {
            var buffer = _documentConverter.Convert(sourceFilePath);

            if (buffer.Length == 0) {
                _logger.LogWarning("Could not convert file. Buffer is empty.");
                return;
            }

            File.WriteAllBytes(destinationFilePath, buffer);
        }

        #endregion

        #region Public Methods

        public void DisplayDocument(string filePath) {
            Title = Path.GetFileNameWithoutExtension(filePath);

            if (!CanDisplay(filePath)) {
                _messageBoxService.Show(title: "Exibir",
                                        message: "Não é possível exibir este documento no visualizador.",
                                        icon: MessageBoxIcon.Exclamation);
                return;
            }

            CurrentDocumentViewer.Document = OpenXpsDocument(filePath)
                .GetFixedDocumentSequence();
        }

        #endregion
    }
}
