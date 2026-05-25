using AmsaAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AmsaAPI.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests that configures an in-memory SQLite database
/// </summary>
public class AmsaApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"amsa_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the default DbContext registration
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AmsaDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory SQLite database for testing
            services.AddDbContext<AmsaDbContext>(options =>
            {
                // Use in-memory SQLite for isolation between tests
                options.UseSqlite($"Data Source=:memory:{_databaseName};");
            });
        });

        builder.UseEnvironment("Test");
    }

    /// <summary>
    /// Create a new scope with a fresh database context for the test
    /// </summary>
    public async Task<AmsaDbContext> CreateDbContextAsync()
    {
        var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AmsaDbContext>();
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
