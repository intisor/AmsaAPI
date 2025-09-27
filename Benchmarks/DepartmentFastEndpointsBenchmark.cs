using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AmsaAPI.Data;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class DepartmentFastEndpointsBenchmark
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
                        options.UseInMemoryDatabase("DepartmentBenchmarkTestDb");
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
        if (context.Departments.Any()) return;

        // Add departments
        var departments = new List<Department>
        {
            new Department { DepartmentId = 1, DepartmentName = "Medical" },
            new Department { DepartmentId = 2, DepartmentName = "Engineering" },
            new Department { DepartmentId = 3, DepartmentName = "Finance" }
        };
        context.Departments.AddRange(departments);

        // Add levels
        var levels = new List<Level>
        {
            new Level { LevelId = 1, LevelType = "Executive" },
            new Level { LevelId = 2, LevelType = "Senior" },
            new Level { LevelId = 3, LevelType = "Junior" }
        };
        context.Levels.AddRange(levels);
        context.SaveChanges();

        // Add level-departments
        var levelDepartments = new List<LevelDepartment>();
        int ldId = 1;
        foreach (var dept in departments)
        {
            foreach (var level in levels)
            {
                levelDepartments.Add(new LevelDepartment
                {
                    LevelDepartmentId = ldId++,
                    LevelId = level.LevelId,
                    DepartmentId = dept.DepartmentId
                });
            }
        }
        context.LevelDepartments.AddRange(levelDepartments);

        // Add national, state, unit for members
        var national = new National { NationalId = 1, NationalName = "Nigeria" };
        context.Nationals.Add(national);
        var state = new State { StateId = 1, StateName = "Lagos", NationalId = 1 };
        context.States.Add(state);
        var unit = new Unit { UnitId = 1, UnitName = "Test Unit", StateId = 1 };
        context.Units.Add(unit);
        context.SaveChanges();

        // Add members with roles
        var members = new List<Member>();
        var memberRoles = new List<MemberLevelDepartment>();
        for (int i = 1; i <= 50; i++)
        {
            var member = new Member
            {
                MemberId = i,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"test{i}@example.com",
                Mkanid = 1000 + i,
                UnitId = 1
            };
            members.Add(member);

            // Assign some roles
            if (i % 2 == 0)
            {
                memberRoles.Add(new MemberLevelDepartment
                {
                    MemberId = i,
                    LevelDepartmentId = 1 // Medical Executive
                });
            }
        }
        context.Members.AddRange(members);
        context.MemberLevelDepartments.AddRange(memberRoles);
        context.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetAllDepartments()
    {
        return await _client.GetAsync("/api/departments");
    }

    [Benchmark]
    public async Task<HttpResponseMessage> GetDepartmentById()
    {
        return await _client.GetAsync("/api/departments/1");
    }
}