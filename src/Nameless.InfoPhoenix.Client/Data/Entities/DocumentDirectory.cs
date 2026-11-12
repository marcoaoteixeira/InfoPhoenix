using System.Diagnostics;
using Nameless.EntityFrameworkCore.Entities;

namespace Nameless.InfoPhoenix.Client.Data.Entities;

/// <summary>
///     Represents a document directory.
/// </summary>
[DebuggerDisplay("{DebuggerDisplayValue,nq}")]
public class DocumentDirectory : EntityBase {
    private string DebuggerDisplayValue => $"{nameof(DirectoryPath)}: {DirectoryPath}";

    public static DocumentDirectory Empty { get; } = new();

    /// <summary>
    ///     Gets or sets the document directory label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document directory path.
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document directory display position.
    /// </summary>
    public int DisplayPosition { get; set; }

    /// <summary>
    ///     Gets or sets the last time the document directory was indexed.
    /// </summary>
    public DateTime? LastIndexingTime { get; set; }

    /// <summary>
    ///     Gets or sets the documents bounded to the document directory.
    /// </summary>
    public ICollection<Document> Documents { get; set; } = [];
}