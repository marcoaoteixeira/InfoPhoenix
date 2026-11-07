using Nameless.InfoPhoenix.Client.Data.DTOs;

namespace Nameless.InfoPhoenix.Client.Infrastructure.Objects;

public record SearchResultsMetadata(
    DocumentDirectoryDto DocumentDirectory,
    string[] HighlightTerms
);
