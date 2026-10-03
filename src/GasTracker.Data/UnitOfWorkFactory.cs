using GasTracker.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data;

public class UnitOfWorkFactory(IDbContextFactory<GasTrackerDbContext> contextFactory) : IUnitOfWorkFactory
{
    public IUnitOfWork Create() => new UnitOfWork(contextFactory.CreateDbContext());
}
