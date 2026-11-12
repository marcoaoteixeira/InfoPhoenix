namespace Nameless.InfoPhoenix.Client.Data.DTOs;

public record DocumentDto {
    public static DocumentDto Empty => new();

    public Guid ID { get; init; }
    
    public string FilePath { get; init; } = string.Empty;

    public string FileName => SysPath.GetFileName(FilePath);
    
    public string Content { get; init; } = string.Empty;
    
    public byte[] RawFile { get; init; } = [];
    
    public DateTime? LastIndexingTime { get; init; }
    
    public DateTime? LastWriteTime { get; init; }
    
    public Guid DocumentDirectoryID { get; init; }
}
