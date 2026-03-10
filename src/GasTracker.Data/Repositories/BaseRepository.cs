using GasTracker.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Repositories;

public abstract class BaseRepository<T>(GasTrackerDbContext context) : IRepository<T> where T : class
{
    protected readonly GasTrackerDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(int id) =>
        await DbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() =>
        await DbSet.ToListAsync();

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is not null)
            DbSet.Remove(entity);
    }
}
