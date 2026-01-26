using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Office;
using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.Documents.ConvertXps;

public class ConvertIntoXpsFormatRequestHandler : IRequestHandler<ConvertIntoXpsFormatRequest, ConvertIntoXpsFormatResponse> {
    private readonly IDocumentService _documentService;
    
    public ConvertIntoXpsFormatRequestHandler(IDocumentService documentService) {
        _documentService = documentService;
    }

    public async Task<ConvertIntoXpsFormatResponse> HandleAsync(ConvertIntoXpsFormatRequest request, CancellationToken cancellationToken) {
        var result = await _documentService.ConvertAsync(
            filePath: request.FilePath,
            output: DocumentType.XPS,
            cancellationToken
        ).SkipContextSync();

        return result.Match<ConvertIntoXpsFormatResponse>(
            onSuccess: value => new ConvertIntoXpsFormatMetadata(value),
            onFailure: errors => errors[0]
        );
    }
}