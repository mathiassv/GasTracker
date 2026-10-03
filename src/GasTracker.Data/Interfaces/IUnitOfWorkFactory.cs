namespace GasTracker.Data.Interfaces;

/// <summary>
/// Creates a short-lived unit of work with its own DbContext. Blazor Server components use this
/// instead of a scoped <see cref="IUnitOfWork"/>, whose context would otherwise live (and keep
/// tracking entities) for the whole circuit.
/// </summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
