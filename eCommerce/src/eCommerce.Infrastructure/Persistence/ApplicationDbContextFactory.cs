using eCommerce.Application.Common.Events;
using eCommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eCommerce.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time. Building the context here rather
/// than booting the Api host keeps migration commands independent of the
/// application's start-up and configuration.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Override when the design-time database differs from the local default,
    /// e.g. <c>set ECOMMERCE_DESIGNTIME_CONNECTION=...</c>.
    /// </summary>
    public const string ConnectionStringVariable = "ECOMMERCE_DESIGNTIME_CONNECTION";

    private const string DefaultConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=eCommerce;Trusted_Connection=True;TrustServerCertificate=True";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        // Scaffolding never dispatches events, so a no-op is enough.
        return new ApplicationDbContext(options, NullDomainEventDispatcher.Instance);
    }

    private sealed class NullDomainEventDispatcher : IDomainEventDispatcher
    {
        public static readonly NullDomainEventDispatcher Instance = new();

        public Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
