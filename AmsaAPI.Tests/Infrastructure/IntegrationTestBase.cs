using AmsaAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests providing common setup and teardown
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly AmsaApiWebApplicationFactory Factory;
    protected HttpClient Client { get; private set; } = null!;
    protected AmsaDbContext DbContext { get; private set; } = null!;

    protected IntegrationTestBase()
    {
        Factory = new AmsaApiWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        DbContext = await Factory.CreateDbContextAsync();
        // EnsureCreatedAsync is already called in Program.cs for Test environment
        // Just ensure we have access to a fresh context
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        DbContext.Dispose();
        Factory.Dispose();
    }

    /// <summary>
    /// Helper to refresh an entity from the database after modifications
    /// </summary>
    protected async Task RefreshEntityAsync<T>(T entity) where T : class
    {
        await DbContext.Entry(entity).ReloadAsync();
    }

    /// <summary>
    /// Helper to verify an entity exists in the database
    /// </summary>
    protected async Task<bool> EntityExistsAsync<T>(Func<IQueryable<T>, IQueryable<T>> predicate) where T : class
    {
        var query = DbContext.Set<T>().AsQueryable();
        query = predicate(query);
        return await query.AnyAsync();
    }
}
