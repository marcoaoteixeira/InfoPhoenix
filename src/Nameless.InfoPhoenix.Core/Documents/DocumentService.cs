using System.IO;
using Nameless.InfoPhoenix.Office;
using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.Documents;

public class DocumentService : IDocumentService {
    private readonly IEnumerable<IDocumentReader> _documentReaders;
    private readonly IEnumerable<IDocumentConverter> _documentConverters;

    public DocumentService(
        IEnumerable<IDocumentReader> documentReaders,
        IEnumerable<IDocumentConverter> documentConverters) {
        _documentReaders = documentReaders;
        _documentConverters = documentConverters;
    }

    public async Task<string> GetContentAsync(string filePath, CancellationToken cancellationToken) {
        Guard.Against.NullOrWhiteSpace(filePath);

        var reader = _documentReaders.FirstOrDefault(reader => reader.CanRead(filePath));

        if (reader is not null) {
            return await reader.GetContentAsync(filePath, cancellationToken)
                               .ConfigureAwait(continueOnCapturedContext: false);
        }

        return string.Empty;
    }

    public async Task<Result<string>> ConvertAsync(string filePath, DocumentType output, CancellationToken cancellationToken) {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var fileType = extension switch {
            ".doc" or ".docx" => DocumentType.DOCX,
            ".rtf" => DocumentType.RTF,
            ".xps" => DocumentType.XPS,
            ".txt" => DocumentType.TXT,
            ".pdf" => DocumentType.PDF,
            _ => DocumentType.None
        };

        if (fileType == DocumentType.None) {
            return Error.Conflict($"A extensão '{extension}' não é suportada.");
        }

        // Find a converter that can handle the specified document type
        var converter = _documentConverters.FirstOrDefault(converter => converter.CanConvert(fileType) && converter.OutputType == output);

        if (converter is null) {
            return Error.Conflict("Não existe um conversor implementado que consiga traduzir o arquivo.");
        }

        return await converter.ConvertAsync(filePath, cancellationToken);
    }
}