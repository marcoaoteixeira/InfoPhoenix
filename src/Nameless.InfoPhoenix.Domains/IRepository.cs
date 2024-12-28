using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains;

public interface IRepository {
    IQueryable<TEntity> GetQuery<TEntity>() where TEntity : EntityBase;

    Task<TEntity> SaveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : EntityBase;

    Task<int> DeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : EntityBase;

    Task CommitChangesAsync(CancellationToken cancellationToken);
}