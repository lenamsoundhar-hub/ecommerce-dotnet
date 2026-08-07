using System.Reflection;
using eCommerce.Application.Common.Events;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options) => _domainEventDispatcher = domainEventDispatcher;

    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Commits the unit of work, then publishes whatever the aggregates
    /// recorded. Events are dispatched after the save so handlers never react to
    /// a transaction that subsequently failed.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToArray();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToArray();

        var affected = await base.SaveChangesAsync(cancellationToken);

        // Cleared before dispatch so a handler that saves again cannot replay them.
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return affected;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
