using System.Diagnostics;
using Nameless.EntityFrameworkCore.Entities;

namespace Nameless.InfoPhoenix.Client.Data.Entities;

/// <summary>
///     Represents a document.
/// </summary>
[DebuggerDisplay("{DebuggerDisplayValue,nq}")]
public class Document : EntityBase {
    private string DebuggerDisplayValue => $"{nameof(FilePath)}: {FilePath}";

    /// <summary>
    ///     Gets or sets the document file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document as raw bytes.
    /// </summary>
    public byte[] RawFile { get; set; } = [];

    /// <summary>
    ///     Gets or sets the last time the document was indexed.
    /// </summary>
    public DateTime? LastIndexingTime { get; set; }

    /// <summary>
    ///     Gets or sets the last time the document was modified.
    /// </summary>
    public DateTime? LastWriteTime { get; set; }

    /// <summary>
    ///     Gets or sets the document directory that the current
    ///     document is bound to.
    /// </summary>
    public Guid DocumentDirectoryID { get; set; }

    /// <summary>
    ///     Gets or sets the <see cref="DocumentDirectory"/> entity
    ///     that this document is bounded to.
    /// </summary>
    internal DocumentDirectory DocumentDirectory { get; set; } = null!;
}