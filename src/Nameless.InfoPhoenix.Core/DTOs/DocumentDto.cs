using System.IO;

namespace Nameless.InfoPhoenix.DTOs;

public record DocumentDto {
    public static DocumentDto Empty => new();

    public Guid ID { get; init; }
    
    public string FilePath { get; init; } = string.Empty;

    public string FileName => Path.GetFileName(FilePath);
    
    public string Content { get; init; } = string.Empty;
    
    public byte[] RawFile { get; init; } = [];
    
    public DateTime? LastIndexingTime { get; init; }
    
    public DateTime? LastWriteTime { get; init; }
    
    public Guid DocumentDirectoryID { get; init; }

    public DocumentDto() { }

    public DocumentDto(Document document) {
        ID = document.ID;
        FilePath = document.FilePath;
        Content = document.Content;
        RawFile = document.RawFile;
        LastIndexingTime = document.LastIndexingTime;
        LastWriteTime = document.LastWriteTime;
        DocumentDirectoryID = document.DocumentDirectoryID;
    }
}
