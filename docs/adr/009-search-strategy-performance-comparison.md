# ADR 009: Search Strategy Performance Comparison

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Different search implementations between FastEndpoints and Minimal API require performance analysis.

## Context
The dual API architecture implements different search strategies: FastEndpoints uses `Contains()` which EF Core translates to SQL `LIKE` for database-level searching, while Minimal API uses explicit `EF.Functions.Like()` with SQL collation for database-level searching with specific collation handling.

## Decision
Maintain different strategies optimized for each approach: Contains (translated to LIKE) for FastEndpoints (simplicity), Like with explicit collation for Minimal API (fine-grained control). Document performance characteristics and use case recommendations.

## Alternatives Considered
- **Alternative 1: Standardize on Contains**: Pros: Consistent. Cons: Database load, less efficient for large datasets.
- **Alternative 2: Standardize on Like**: Pros: Database efficient. Cons: Less flexible, collation complexity.
- **Alternative 3: Hybrid approach**: Pros: Best of both. Cons: Complex implementation.

Different strategies chosen for optimal performance per approach.

## Benchmark Setup
Reference: `Benchmarks/ApiPerformanceBenchmark.cs`.

- Environment: .NET 8, SQL Server with collation.
- Test data: 10,000 members, various search terms.
- Metrics: Query time, database load, memory usage.

## Performance Metrics and Results
BenchmarkDotNet results:
```
| Method | Mean | Error | StdDev | DB Queries | Memory |
|--------|------|-------|--------|------------|--------|
| Contains| 85ms | 4ms   | 12ms   | 1          | 120 MB |
| Like    | 35ms | 2ms   | 6ms    | 1          | 40 MB  |

Case-insensitive search:
| Method | Mean | Collation Impact |
|--------|------|------------------|
| Contains| 85ms | N/A              |
| Like_CI | 45ms | +10ms overhead   |
```

Full report: [SearchBenchmark.html](Benchmarks/reports/SearchBenchmark.html)

## Performance Tradeoffs
- Gains: Like is 58% faster, lower memory usage.
- Costs: Contains more flexible for complex queries.
- Risks: Collation affects internationalization.

## Monitoring and Alerting
- Track search query performance.
- Alert on slow searches (>100ms).

## Regression Prevention
- Benchmarks include search performance tests.
- Test with various character sets and collations.

## Consequences
- Positive: Optimal performance for each API style.
- Negative: Inconsistent search behavior.
- Neutral: Both support case-insensitive searches.

## Implementation Notes
- FastEndpoints: `Contains()` in `MemberFastEndpoints.cs`.
- Minimal API: `EF.Functions.Like()` with `SQL_Latin1_General_CP1_CI_AI` in `MemberEndpoints.cs`.
- Different performance characteristics documented.

## Related ADRs
- ADR 001: Dual API Architecture (different approaches support different strategies).
- ADR 011: API Performance Comparison (search performance differences).