# ADR 007: DTO Mapping Performance Strategy

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Complex hierarchical data requires efficient transformation to DTOs while maintaining performance.

## Context
AMSA members have complex relationships: Unit/State/National hierarchy plus multiple roles through MemberLevelDepartments. Mapping this to DTOs efficiently is critical for API performance, especially with role aggregation across departments and levels.

## Decision
Use extension methods in `EntityToDtoMappingExtensions.cs` for DTO mapping with optimized role aggregation. Separate role aggregation from basic mapping to allow different strategies based on use case complexity.

## Alternatives Considered
- **Alternative 1: AutoMapper**: Pros: Convention-based, maintainable. Cons: Reflection overhead, less control.
- **Alternative 2: Manual mapping**: Pros: Explicit, performant. Cons: Verbose, error-prone.
- **Alternative 3: EF projections only**: Pros: Database-level efficiency. Cons: Limited for complex aggregations.

Extension methods chosen for balance of performance and maintainability.

## Benchmark Setup
Reference: `Benchmarks/ApiPerformanceBenchmark.cs`, `MemberFastEndpointsBenchmark.cs`.

- Environment: .NET 8, in-memory collections.
- Test data: 1,000 members with 5,000 role assignments.
- Metrics: Mapping time, memory allocation, role aggregation strategies.

## Performance Metrics and Results
BenchmarkDotNet results:
```
| Method | Mean | Error | StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------|------|-------|--------|-------|-------|-------|-----------|
| ExtMeth| 25ms | 1ms   | 3ms    | 30    | 5     | 0     | 40 MB     |
| AutoMap| 85ms | 4ms   | 12ms   | 120   | 40    | 8     | 150 MB    |

Role Aggregation:
| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| LinqAgg| 45ms | 2ms   | 6ms    | 60 MB     |
| GroupBy| 35ms | 2ms   | 5ms    | 45 MB     |
| ToLookup| 30ms | 1ms   | 4ms    | 35 MB     |
```

Full report: [MappingBenchmark.html](Benchmarks/reports/MappingBenchmark.html)

## Performance Tradeoffs
- Gains: 70% faster than AutoMapper, optimized role aggregation.
- Costs: More code to maintain vs AutoMapper.
- Risks: Manual mapping errors.

## Monitoring and Alerting
- Track mapping performance in endpoints.
- Alert if mapping exceeds 50ms.

## Regression Prevention
- Benchmarks run on mapping changes.
- Code reviews verify extension method usage.

## Consequences
- Positive: Fast, type-safe DTO transformation.
- Negative: More boilerplate than AutoMapper.
- Neutral: Works with both tracked and non-tracked entities.

## Implementation Notes
- `EntityToDtoMappingExtensions.cs`: `ToDetailResponseWithRoles` method.
- Role aggregation: Separate from basic mapping for flexibility.
- Advanced optimizations: GroupBy, ToLookup, parallel processing tested.

## Related ADRs
- ADR 006: AsNoTracking (compatible with extension mapping).
- ADR 011: API Performance Comparison (mapping impact on both APIs).