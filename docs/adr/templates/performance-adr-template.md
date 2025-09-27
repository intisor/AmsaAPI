# ADR [NUMBER]: [TITLE] - Performance Decision

**Status**: Proposed

**Date**: [YYYY-MM-DD]

**Technical Story**: Describe the performance issue or optimization opportunity.

## Context
Background on the performance concern. Reference relevant parts of `ARCHITECTURE.md` or codebase (e.g., EF Core queries in endpoints).

## Decision
Chosen performance approach. E.g., "Use EF Core Include/ThenInclude for eager loading instead of lazy loading."

## Alternatives Considered
- **Alternative 1**: E.g., Raw SQL queries – faster but less maintainable.
- **Alternative 2**: E.g., Caching layer – adds complexity.
- etc.

## Benchmark Setup
Reference: `Benchmarks/BenchmarkRunner.cs` and `Benchmarks/ApiPerformanceBenchmark.cs`.

Describe how benchmarks were run:
- Environment: .NET 8, hardware specs.
- Test data: Sample size, hierarchy depth.
- Metrics measured: Execution time, memory usage, CPU.

## Performance Metrics and Results
Include BenchmarkDotNet results:
```
Benchmark Results:
- Original approach: 150ms avg, 200MB peak memory
- New approach: 50ms avg, 100MB peak memory
Improvement: 67% faster, 50% less memory
```

Link to full reports: [Benchmark Report](path/to/report.html)

## Performance Tradeoffs
- Gains: Faster API response for hierarchy endpoint.
- Costs: Slightly more complex query setup.
- Risks: Potential over-fetching if hierarchy grows.

## Monitoring and Alerting
- Add performance counters in Application Insights.
- Set alerts for response time > 100ms.

## Regression Prevention
- Integrate benchmarks into CI/CD.
- Periodic re-benchmarking.

## Consequences
- Positive/Negative/Neutral as in standard template.

## Implementation Notes
- Code changes in `FastEndpoints/StatisticsFastEndpoints.cs`.
- Benchmark integration.

## Related ADRs
- ADR 001: API Architecture.

---

*Guidance*: Always include quantifiable benchmarks. Reference existing BenchmarkDotNet setup.
