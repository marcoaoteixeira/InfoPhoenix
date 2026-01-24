namespace Nameless.InfoPhoenix.Models;

public record DocumentDirectorySearchEntry {
    public static DocumentDirectorySearchEntry Empty { get; } = new();

    public string Label { get; init; } = string.Empty;

    public DateTime LastIndexingTime { get; init; }

    public DocumentSearchEntry[] Documents { get; init; } = [];

    public int DocumentCount => Documents.Length;
}

public record DocumentSearchEntry {

}