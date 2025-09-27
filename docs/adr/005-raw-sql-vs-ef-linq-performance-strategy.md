# ADR 005: Raw SQL vs EF LINQ Performance Strategy

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Complex hierarchical statistics queries require optimal performance for dashboard and analytics endpoints.

## Context
The AMSA API needs to provide fast statistics for organizational hierarchies (National → State → Unit → Member) with role aggregations. Standard EF LINQ queries struggle with complex joins and aggregations, leading to N+1 query problems and poor performance.

## Decision
Use raw SQL for complex statistics queries while maintaining EF LINQ for standard CRUD operations. Raw SQL provides optimal performance for aggregation-heavy endpoints like unit stats, department stats, and organization summaries.

## Alternatives Considered
- **Alternative 1: Pure EF LINQ with Includes**: Pros: Type safety, maintainability. Cons: N+1 queries, poor performance for complex hierarchies.
- **Alternative 2: Stored procedures**: Pros: Optimized execution plans. Cons: Harder to maintain, less portable.
- **Alternative 3: Database views**: Pros: Pre-computed aggregations. Cons: Complex to update, schema coupling.

Raw SQL chosen for balance of performance and maintainability.

## Benchmark Setup
Reference: `Benchmarks/ApiPerformanceBenchmark.cs`.

- Environment: .NET 8, SQL Server LocalDB.
- Test data: 5 nationals, 50 states, 500 units, 10,000 members.
- Metrics: Query execution time, memory usage, SQL roundtrips.

## Performance Metrics and Results
BenchmarkDotNet results:
```
| Method | Mean | Error | StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------|------|-------|--------|-------|-------|-------|-----------|
| RawSQL | 45ms | 2ms   | 6ms    | 20    | 5     | 0     | 50 MB     |
| EFLinq | 245ms| 12ms  | 35ms   | 150   | 50    | 10    | 200 MB    |

Improvement: 82% faster, 75% less memory, reduced from 50+ queries to 1.
```

Full report: [StatisticsBenchmark.html](Benchmarks/reports/StatisticsBenchmark.html)

## Performance Tradeoffs
- Gains: Dramatic performance improvement for analytics.
- Costs: SQL injection risk (mitigated by parameterization), less type safety.
- Risks: Schema changes require SQL updates.

## Monitoring and Alerting
- Track query performance in Application Insights.
- Alert if statistics endpoints exceed 100ms.

## Regression Prevention
- Include statistics benchmarks in CI/CD.
- Re-benchmark after schema changes.

## Consequences
- Positive: Fast dashboard loading, scalable analytics.
- Negative: SQL maintenance overhead.
- Neutral: CRUD operations remain EF LINQ.

## Implementation Notes
- Raw SQL in `StatisticsEndpoints.cs`: `GetUnitStats`, `GetDepartmentStats` using SqlQueryRaw with parameterization via FormattableString for queries with parameters.
- Parameterized queries prevent injection.
- EF LINQ for `MemberEndpoints.cs` CRUD operations.

## Related ADRs
- ADR 003: Database Design (normalized schema supports raw SQL).
- ADR 011: API Performance Comparison (raw SQL impact).