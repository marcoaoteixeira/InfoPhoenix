using System.IO;
using DomainRoot = Nameless.InfoPhoenix.Domain.Root;

namespace Nameless.InfoPhoenix.Client.Resources {
    internal static class ResourceHelper {
        #region Internal Static Methods

        internal static string GetDocumentIcon(string filePath)
            => Path.GetExtension(filePath) switch {
                DomainRoot.Files.Extensions.DOC => "/Resources/files/doc_file.png",
                DomainRoot.Files.Extensions.DOCX => "/Resources/files/docx_file.png",
                DomainRoot.Files.Extensions.RTF => "/Resources/files/rtf_file.png",
                DomainRoot.Files.Extensions.TXT => "/Resources/files/txt_file.png",
                DomainRoot.Files.Extensions.PDF => "/Resources/files/pdf_file.png",
                DomainRoot.Files.Extensions.XPS => "/Resources/files/xps_file.png",
                _ => "/Resources/files/doc_file.png",
            };

        #endregion
    }
}
