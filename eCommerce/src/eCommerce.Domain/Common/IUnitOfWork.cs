namespace eCommerce.Domain.Common;

/// <summary>
/// Commits the changes tracked across one or more repositories as a single
/// transaction. Handlers call this once, at the end of a use case.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
