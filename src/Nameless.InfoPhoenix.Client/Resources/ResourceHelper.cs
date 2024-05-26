using System.IO;
using DomainRoot = Nameless.InfoPhoenix.Domain.Root;

namespace Nameless.InfoPhoenix.Client.Resources {
    internal static class ResourceHelper {
        #region Internal Static Methods

        internal static string GetDocumentIcon(string filePath)
            => Path.GetExtension(filePath) switch {
                DomainRoot.Files.Extensions.DOC => "pack://application:,,,/Resources/files/doc_file.png",
                DomainRoot.Files.Extensions.DOCX => "pack://application:,,,/Resources/files/docx_file.png",
                DomainRoot.Files.Extensions.RTF => "pack://application:,,,/Resources/files/rtf_file.png",
                DomainRoot.Files.Extensions.TXT => "pack://application:,,,/Resources/files/txt_file.png",
                DomainRoot.Files.Extensions.PDF => "pack://application:,,,/Resources/files/pdf_file.png",
                DomainRoot.Files.Extensions.XPS => "pack://application:,,,/Resources/files/xps_file.png",
                _ => "pack://application:,,,/Resources/files/doc_file.png",
            };

        #endregion
    }
}
