using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.UseCases.Documents.ConvertXps;

public record ConvertIntoXpsFormatRequest : IRequest<ConvertIntoXpsFormatResponse> {
    public required string FilePath { get; init; }
}