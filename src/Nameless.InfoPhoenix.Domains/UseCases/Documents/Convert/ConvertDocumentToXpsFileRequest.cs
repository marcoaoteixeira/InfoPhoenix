using MediatR;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Domains.UseCases.Documents.Convert;

public sealed record ConvertDocumentToXpsFileRequest : IRequest<Result<string>> {
    public IProgress<string> Reporter { get; init; } = NullProgress<string>.Instance;
    public string FilePath { get; init; } = string.Empty;
}