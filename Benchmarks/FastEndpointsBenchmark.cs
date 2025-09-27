using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Threading.Tasks;
using AmsaAPI.Data;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class FastEndpointsBenchmark
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
                        services.Remove(descriptor);

                    // Add in-memory database for testing
                    services.AddDbContext<AmsaDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("FastEndpointsBenchmarkDb");
                    });
                });
            });
        _client = _factory.CreateClient();

        // Seed test data
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AmsaDbContext>();
        dbContext.Database.EnsureCreated();
        SeedTestData(dbContext);
    }

    private static void SeedTestData(AmsaDbContext db)
    {
        if (db.Nationals.Any()) return;
        var national = new National { NationalName = "Test National" };
        db.Nationals.Add(national);
        db.SaveChanges();
        var state = new State { StateName = "Test State", NationalId = national.NationalId };
        db.States.Add(state);
        db.SaveChanges();
        var unit = new Unit { UnitName = "Test Unit", StateId = state.StateId };
        db.Units.Add(unit);
        db.SaveChanges();
        var members = Enumerable.Range(1, 100).Select(i => new Member
        {
            FirstName = $"First{i}",
            LastName = $"Last{i}",
            Email = $"test{i}@example.com",
            Mkanid = 1000 + i,
            UnitId = unit.UnitId
        }).ToList();
        db.Members.AddRange(members);
        db.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllMembers() => await _client.GetAsync("/api/members");

    [Benchmark]
    public async Task<HttpResponseMessage> GetDashboardStats() => await _client.GetAsync("/api/stats/dashboard");

    [Benchmark]
    public async Task<HttpResponseMessage> GetOrganizationSummary() => await _client.GetAsync("/api/stats/organization-summary");

    [Benchmark]
    public async Task<HttpResponseMessage> GetHierarchy() => await _client.GetAsync("/api/hierarchy");

    [Benchmark]
    public async Task<HttpResponseMessage> SearchMembersByName() => await _client.GetAsync("/api/members/search/First1");

    [Benchmark]
    public async Task<HttpResponseMessage> GetMembersByUnit() => await _client.GetAsync("/api/members/unit/1");

    [Benchmark]
    public async Task<HttpResponseMessage> GetMembersByDepartment() => await _client.GetAsync("/api/members/department/1");
}
