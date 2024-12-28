using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains;

public sealed class Repository : IRepository {
    private readonly AppDbContext _dbContext;

    public Repository(AppDbContext dbContext) {
        _dbContext = Prevent.Argument.Null(dbContext);
    }

    public IQueryable<TEntity> GetQuery<TEntity>() where TEntity : EntityBase
        => _dbContext.Set<TEntity>();

    public async Task<TEntity> SaveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : EntityBase {
        var entry = entity.ID == Guid.Empty
            ? await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken)
            : _dbContext.Set<TEntity>().Update(entity);

        return entry.Entity;
    }

    public async Task<int> DeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : EntityBase
        => await _dbContext.Set<TEntity>()
                           .Where(entity => entity.ID == id)
                           .ExecuteDeleteAsync(cancellationToken);

    public Task CommitChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}