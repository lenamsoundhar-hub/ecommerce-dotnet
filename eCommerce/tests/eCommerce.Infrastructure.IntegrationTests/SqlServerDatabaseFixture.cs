using eCommerce.Application.Common.Events;
using eCommerce.Infrastructure.Persistence;
using eCommerce.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Infrastructure.IntegrationTests;

/// <summary>
/// Creates one throwaway SQL Server database for the test assembly and applies
/// the real migrations to it.
/// <para>
/// These tests run against a relational provider on purpose. The EF in-memory
/// provider evaluates LINQ in memory, so it silently accepts queries and value
/// conversions that fail once they have to become SQL — which is exactly the
/// class of defect this project exists to catch.
/// </para>
/// </summary>
public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    /// <summary>Point the suite at another server, e.g. a CI container.</summary>
    public const string ConnectionStringVariable = "ECOMMERCE_TEST_CONNECTION";

    private const string DefaultMasterConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _databaseName = $"eCommerce_IntegrationTests_{Guid.NewGuid():N}";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var master = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? DefaultMasterConnectionString;

        ConnectionString = new SqlConnectionStringBuilder(master)
        {
            InitialCatalog = _databaseName,
        }.ConnectionString;

        await using var context = CreateContext();

        // Runs the committed migrations rather than EnsureCreated, so the tests
        // exercise the same schema the application deploys.
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Builds a context against the test database. Pass a dispatcher to observe
    /// domain events, or omit it for a no-op.
    /// </summary>
    public ApplicationDbContext CreateContext(
        IDomainEventDispatcher? dispatcher = null,
        TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new AuditableEntityInterceptor(timeProvider ?? TimeProvider.System))
            .Options;

        return new ApplicationDbContext(options, dispatcher ?? new RecordingDomainEventDispatcher());
    }

    /// <summary>Empties the catalogue so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        await context.Database.ExecuteSqlRawAsync("DELETE FROM [Products]");
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SqlServer";
}
