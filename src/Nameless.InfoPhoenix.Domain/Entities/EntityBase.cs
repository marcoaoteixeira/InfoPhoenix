namespace Nameless.InfoPhoenix.Domain.Entities {
    public abstract class EntityBase {
        #region Public Virtual Properties

        public virtual Guid ID { get; set; }
        public virtual DateTime CreatedAt { get; set; }
        public virtual DateTime? ModifiedAt { get; set; }

        #endregion
    }
}
