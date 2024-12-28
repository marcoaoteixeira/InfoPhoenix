namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public sealed record SearchDocumentsResponse {
    public SearchResultCollection Value { get; init; } = [];
    public string[] HighlightTerms { get; init; } = [];
}