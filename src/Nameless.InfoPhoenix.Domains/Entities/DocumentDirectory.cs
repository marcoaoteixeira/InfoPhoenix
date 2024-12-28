using System.Diagnostics;

namespace Nameless.InfoPhoenix.Domains.Entities;

[DebuggerDisplay("{DebuggerDisplayValue,nq}")]
public sealed class DocumentDirectory : EntityBase {
    private string DebuggerDisplayValue
        => $"{nameof(ID)}: {ID}, {nameof(Label)}: {Label}, {nameof(DirectoryPath)}: {DirectoryPath}";

    public string Label { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime? LastIndexingTime { get; set; }
}