using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AuthDbContext Context;
    protected readonly DbSet<T> DbSet;

    public BaseRepository(AuthDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
    {
        return await DbSet.FindAsync([id], ct);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await DbSet.Where(predicate).ToListAsync(ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await Context.SaveChangesAsync(ct);
    }
}
