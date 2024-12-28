using System.Diagnostics;

namespace Nameless.InfoPhoenix.Domains.Entities;

[DebuggerDisplay("{DebuggerDisplayValue,nq}")]
public sealed class Document : EntityBase {
    private string DebuggerDisplayValue
        => $"{nameof(ID)}: {ID}, {nameof(DocumentDirectoryID)}: {DocumentDirectoryID}, {nameof(FilePath)}: {FilePath}";

    public Guid DocumentDirectoryID { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public byte[] RawFile { get; set; } = [];
    public DateTime? LastIndexingTime { get; set; }
    public DateTime? LastWriteTime { get; set; }
}