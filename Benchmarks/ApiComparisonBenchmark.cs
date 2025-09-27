using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AmsaAPI.Data;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ApiComparisonBenchmark
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AmsaDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<AmsaDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("ApiComparisonTestDb");
                    });
                });
            });

        _client = _factory.CreateClient();

        // Seed test data once
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AmsaDbContext>();
        dbContext.Database.EnsureCreated();
        SeedTestData(dbContext);
    }

    private static void SeedTestData(AmsaDbContext context)
    {
        if (context.Nationals.Any()) return;

        var national = new National { NationalName = "Test National" };
        context.Nationals.Add(national);
        context.SaveChanges();

        var state = new State { StateName = "Test State", NationalId = national.NationalId };
        context.States.Add(state);
        context.SaveChanges();

        var unit = new Unit { UnitName = "Test Unit", StateId = state.StateId };
        context.Units.Add(unit);
        context.SaveChanges();

        // Add test members for meaningful benchmarks
        var members = Enumerable.Range(1, 100).Select(i => new Member
        {
            FirstName = $"First{i}",
            LastName = $"Last{i}",
            Email = $"test{i}@example.com",
            Mkanid = 1000 + i,
            UnitId = unit.UnitId
        }).ToList();

        context.Members.AddRange(members);
        context.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<HttpResponseMessage> FastEndpoints_GetMembers()
    {
        return await _client.GetAsync("/api/members");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> FastEndpoints_GetDashboardStats()
    {
        return await _client.GetAsync("/api/stats/dashboard");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> FastEndpoints_GetOrganizationSummary()
    {
        return await _client.GetAsync("/api/stats/organization-summary");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> FastEndpoints_GetHierarchy()
    {
        return await _client.GetAsync("/api/hierarchy");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> MinimalAPI_GetDashboardStats()
    {
        return await _client.GetAsync("/api/minimal/stats/dashboard");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> MinimalAPI_GetOrganizationSummary()
    {
        return await _client.GetAsync("/api/minimal/stats/organization-summary");
    }
}