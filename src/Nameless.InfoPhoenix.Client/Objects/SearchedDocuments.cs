using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Client.Objects;

public sealed record SearchedDocuments {
    public DocumentDto[] Documents { get; init; } = [];
    public string[] HighlightTerms { get; init; } = [];
}