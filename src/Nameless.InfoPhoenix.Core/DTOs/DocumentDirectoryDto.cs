using Nameless.InfoPhoenix.Entities;

namespace Nameless.InfoPhoenix.DTOs;

public record DocumentDirectoryDto {
    public static DocumentDirectoryDto Empty => new();

    public Guid ID { get; init; }

    public string Label { get; init; } = string.Empty;

    public string DirectoryPath { get; init; } = string.Empty;

    public int DisplayPosition { get; init; }

    public DateTime? LastIndexingTime { get; init; }

    public DocumentDto[] Documents { get; init; } = [];

    public int DocumentCount { get; init; }

    public DocumentDirectoryDto() { }

    public DocumentDirectoryDto(DocumentDirectory documentDirectory) {
        ID = documentDirectory.ID;
        Label = documentDirectory.Label;
        DirectoryPath = documentDirectory.DirectoryPath;
        DisplayPosition = documentDirectory.DisplayPosition;
        LastIndexingTime = documentDirectory.LastIndexingTime;
    }
}