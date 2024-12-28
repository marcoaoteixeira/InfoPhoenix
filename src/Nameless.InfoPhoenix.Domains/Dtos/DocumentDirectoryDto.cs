namespace Nameless.InfoPhoenix.Domains.Dtos;

public sealed record DocumentDirectoryDto {
    public static DocumentDirectoryDto Empty => new();

    public Guid ID { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DirectoryPath { get; init; } = string.Empty;
    public int Order { get; init; }
    public DateTime? LastIndexingTime { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }

    public int DocumentCount { get; init; }
}