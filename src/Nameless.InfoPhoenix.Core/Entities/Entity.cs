namespace Nameless.InfoPhoenix.Entities;

/// <summary>
///     Represents an entity.
/// </summary>
public abstract class Entity {
    /// <summary>
    ///     Gets or sets the ID.
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    ///     Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the last modified time.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}