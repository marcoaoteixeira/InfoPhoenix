using Nameless.InfoPhoenix.DTOs;

namespace Nameless.InfoPhoenix.Models;

public record SearchResultsMetadata(
    DocumentDirectoryDto DocumentDirectory,
    string[] HighlightTerms
);
