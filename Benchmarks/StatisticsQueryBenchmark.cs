using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using AmsaAPI.Data;
using AmsaAPI.DTOs;

namespace AmsaAPI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class StatisticsQueryBenchmark
{
    private AmsaDbContext _dbContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AmsaDbContext>()
            .UseInMemoryDatabase(databaseName: "BenchmarkTestDb")
            .Options;
        _dbContext = new AmsaDbContext(options);

        // Seed test data for benchmarking
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        // Add test data
        var national = new National { NationalId = 1, NationalName = "Nigeria" };
        _dbContext.Nationals.Add(national);

        var state = new State { StateId = 1, StateName = "Lagos", NationalId = 1 };
        _dbContext.States.Add(state);

        // Add test units with members
        for (int i = 1; i <= 50; i++)
        {
            var unit = new Unit
            {
                UnitId = i,
                UnitName = $"Test Unit {i}",
                StateId = 1
            };
            _dbContext.Units.Add(unit);

            // Add members to each unit
            for (int j = 1; j <= 20; j++)
            {
                var member = new Member
                {
                    MemberId = (i - 1) * 20 + j,
                    FirstName = $"First{j}",
                    LastName = $"Last{j}",
                    Email = $"test{j}@unit{i}.com",
                    Mkanid = 1000 + (i - 1) * 20 + j,
                    UnitId = i
                };
                _dbContext.Members.Add(member);
            }
        }

        _dbContext.SaveChanges();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Baseline EF LINQ query with navigation properties and Count() aggregation.
    /// Measures standard ORM approach performance.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<List<UnitStatsDto>> EFLinq_BasicQuery()
    {
        return await _dbContext.Units
            .Include(u => u.State)
            .Select(u => new UnitStatsDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                StateName = u.State.StateName,
                MemberCount = u.Members.Count(),
                ExcoCount = 0 // Simplified for benchmark
            })
            .ToListAsync();
    }

    /// <summary>
    /// Optimized LINQ query using server-side subqueries for counts.
    /// Avoids loading full collections, computes aggregations in database.
    /// </summary>
    [Benchmark]
    public async Task<List<UnitStatsDto>> EFLinq_OptimizedQuery()
    {
        // Pre-load data to avoid N+1 queries
        return await _dbContext.Units
            .Include(u => u.State)
            .Include(u => u.Members)
            .Select(u => new UnitStatsDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                StateName = u.State.StateName,
                MemberCount = u.Members.Count,
                ExcoCount = 0
            })
            .AsNoTracking()
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<UnitStatsDto>> EFLinq_SeparateAggregation()
    {
        // Separate aggregation query
        var memberCounts = await _dbContext.Members
            .GroupBy(m => m.UnitId)
            .Select(g => new { UnitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UnitId, x => x.Count);

        return await _dbContext.Units
            .Include(u => u.State)
            .Select(u => new UnitStatsDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                StateName = u.State.StateName,
                MemberCount = memberCounts.ContainsKey(u.UnitId) ? memberCounts[u.UnitId] : 0,
                ExcoCount = 0
            })
            .AsNoTracking()
            .ToListAsync();
    }

    [Benchmark]
    public List<UnitStatsDto> EFLinq_SynchronousQuery()
    {
        // Test synchronous vs async performance
        return _dbContext.Units
            .Include(u => u.State)
            .Select(u => new UnitStatsDto
            {
                UnitId = u.UnitId,
                UnitName = u.UnitName,
                StateName = u.State.StateName,
                MemberCount = u.Members.Count(),
                ExcoCount = 0
            })
            .AsNoTracking()
            .ToList();
    }
}