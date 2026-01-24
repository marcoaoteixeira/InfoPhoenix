using Nameless.InfoPhoenix.DTOs;

namespace Nameless.InfoPhoenix.UseCases.Search;

public record SearchDocumentsMetadata(
    string SearchTerm,
    string[] HighlightTerms,
    DocumentDirectoryDto[] DocumentDirectories,
    int Count
) {
    public static SearchDocumentsMetadata Empty => new(
        SearchTerm: string.Empty,
        HighlightTerms: [],
        DocumentDirectories: [],
        Count: 0
    );
}