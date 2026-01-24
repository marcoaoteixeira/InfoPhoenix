namespace Nameless.InfoPhoenix.Models;

public record SearchDocument {
    public required Guid DocumentID { get; init; }

    public required Guid DocumentDirectoryID { get; init; }

    public required string FilePath { get; init; }

    public required string Content { get; init; }

    public required DateTime LastIndexingTime { get; init; }
}
