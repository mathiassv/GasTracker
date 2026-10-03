using GasTracker.Data.Interfaces;
using Microsoft.AspNetCore.Components;

namespace GasTracker.Web.Components;

/// <summary>
/// Base for pages that touch the database. Each component instance (i.e. each page visit) gets
/// its own unit of work, disposed with the component, rather than one DbContext per circuit.
/// </summary>
public abstract class UnitOfWorkComponentBase : ComponentBase, IDisposable
{
    [Inject] private IUnitOfWorkFactory UowFactory { get; set; } = null!;

    private IUnitOfWork? _uow;

    protected IUnitOfWork Uow => _uow ??= UowFactory.Create();

    public void Dispose()
    {
        _uow?.Dispose();
        GC.SuppressFinalize(this);
    }
}
