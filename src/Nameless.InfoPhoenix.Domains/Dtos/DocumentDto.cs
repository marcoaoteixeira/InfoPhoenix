namespace Nameless.InfoPhoenix.Domains.Dtos;
public sealed record DocumentDto {
    public Guid ID { get; set; }
    public Guid DocumentDirectoryID { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public byte[] RawFile { get; init; } = [];
    public DateTime? LastIndexingTime { get; init; }
    public DateTime? LastWriteTime { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
