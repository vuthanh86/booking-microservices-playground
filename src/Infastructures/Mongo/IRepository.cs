using System.Linq.Expressions;

using BuildingBlocks.Core.Model;

using JetBrains.Annotations;

namespace BuildingBlocks.Mongo;

public interface IReadRepository<TEntity, in TId>
    where TEntity : class, IAggregate<TId>
{
    [ItemCanBeNull]
    Task<TEntity> FindByIdAsync(TId id, CancellationToken cancellationToken = default);

    [ItemCanBeNull]
    Task<TEntity> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TEntity>> RawQuery(
        string query,
        CancellationToken cancellationToken = default,
        params object[] queryParams);
}

public interface IWriteRepository<TEntity, in TId>
    where TEntity : class, IAggregate<TId>
{
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default);
}

public interface IRepository<TEntity, in TId> :
    IReadRepository<TEntity, TId>,
    IWriteRepository<TEntity, TId>,
    IDisposable
    where TEntity : class, IAggregate<TId>
{ }

public interface IRepository<TEntity> : IRepository<TEntity, long>
    where TEntity : class, IAggregate<long>
{ }
