# ADR 000: Example Performance ADR - EF Core Eager Loading for Hierarchy Endpoint

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: The organization hierarchy endpoint in StatisticsFastEndpoints.cs suffers from N+1 queries when building the tree structure.

## Context
See `ARCHITECTURE.md` for API structure. Current implementation uses lazy loading, causing multiple DB roundtrips for states/units/members.

## Decision
Switch to eager loading with Include/ThenInclude in the query. Project directly to DTOs in memory to avoid over-fetching.

## Alternatives Considered
- **Alternative 1: Raw SQL**: Pros: Single query, fastest. Cons: Loses EF type safety, harder to maintain with schema changes.
- **Alternative 2: Separate queries with caching**: Pros: Modular. Cons: Cache invalidation complexity, still multiple calls.
- **Alternative 3: As-is lazy loading**: Pros: Simple. Cons: Poor scalability for large hierarchies.

Eager loading chosen for balance of performance and maintainability.

## Benchmark Setup
Reference: `Benchmarks/BenchmarkRunner.cs` and `Benchmarks/ApiPerformanceBenchmark.cs`.

- Environment: .NET 8, Intel i7, 16GB RAM, SQL Server LocalDB.
- Test data: 5 nationals, 50 states, 500 units, 10,000 members.
- Metrics: Endpoint response time, query count, memory usage.
- Run: `dotnet run -c Release --project Benchmarks`

## Performance Metrics and Results
BenchmarkDotNet results:
```
| Method | Mean | Error | StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------|------|-------|--------|-------|-------|-------|-----------|
| Lazy   | 245ms| 12ms  | 35ms   | 150   | 50    | 10    | 200 MB    |
| Eager  | 45ms | 2ms   | 6ms    | 20    | 5     | 0     | 50 MB     |

Improvement: 82% faster, 75% less memory, reduced from 500+ queries to 1.
```

Full report: [HierarchyBenchmark.html](Benchmarks/reports/HierarchyBenchmark.html)

## Performance Tradeoffs
- Gains: Faster API, better scalability.
- Costs: Slightly more complex query if adding filters later.
- Risks: Over-fetching if including unnecessary members (mitigated by AsNoTracking).

## Monitoring and Alerting
- Track endpoint duration in Application Insights.
- Alert if >100ms average.

## Regression Prevention
- Add benchmark to CI pipeline.
- Re-run quarterly or after DB changes.

## Consequences
- Positive: Improved user experience for dashboard.
- Negative: Initial learning curve for EF Includes.
- Neutral: No impact on other endpoints.

## Implementation Notes
- Updated `FastEndpoints/StatisticsFastEndpoints.cs`: Use Include(n => n.States).ThenInclude(s => s.Units).ThenInclude(u => u.Members).
- Verified with `dotnet build` and unit tests.

## Related ADRs
- ADR 001: API Performance Guidelines (from ARCHITECTURE.md).
