using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AmsaAPI.Data;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class OrganizationFastEndpointsBenchmark
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
                        options.UseInMemoryDatabase("OrganizationBenchmarkTestDb");
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

        // Add nationals
        var nationals = new List<National>
        {
            new National { NationalId = 1, NationalName = "Nigeria" },
            new National { NationalId = 2, NationalName = "Ghana" }
        };
        context.Nationals.AddRange(nationals);
        context.SaveChanges();

        // Add states
        var states = new List<State>
        {
            new State { StateId = 1, StateName = "Lagos", NationalId = 1 },
            new State { StateId = 2, StateName = "Abuja", NationalId = 1 },
            new State { StateId = 3, StateName = "Accra", NationalId = 2 }
        };
        context.States.AddRange(states);
        context.SaveChanges();

        // Add units
        var units = new List<Unit>
        {
            new Unit { UnitId = 1, UnitName = "Lagos State University", StateId = 1 },
            new Unit { UnitId = 2, UnitName = "University of Lagos", StateId = 1 },
            new Unit { UnitId = 3, UnitName = "Abuja University", StateId = 2 },
            new Unit { UnitId = 4, UnitName = "Accra University", StateId = 3 }
        };
        context.Units.AddRange(units);
        context.SaveChanges();

        // Add members to units
        var members = new List<Member>();
        for (int i = 1; i <= 100; i++)
        {
            var unitId = (i % 4) + 1; // Distribute across units
            members.Add(new Member
            {
                MemberId = i,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"test{i}@example.com",
                Mkanid = 1000 + i,
                UnitId = unitId
            });
        }
        context.Members.AddRange(members);
        context.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllUnits()
    {
        return await _client.GetAsync("/api/units");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetUnitById()
    {
        return await _client.GetAsync("/api/units/1");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetUnitsByState()
    {
        return await _client.GetAsync("/api/units/state/1");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllStates()
    {
        return await _client.GetAsync("/api/states");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetStateById()
    {
        return await _client.GetAsync("/api/states/1");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllNationals()
    {
        return await _client.GetAsync("/api/nationals");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetNationalById()
    {
        return await _client.GetAsync("/api/nationals/1");
    }
}