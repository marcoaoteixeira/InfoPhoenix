namespace Nameless.InfoPhoenix.Domains.Entities;

public abstract class EntityBase {
    public virtual Guid ID { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime? ModifiedAt { get; set; }
}