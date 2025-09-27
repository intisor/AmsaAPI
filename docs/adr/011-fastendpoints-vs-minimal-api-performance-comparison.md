# ADR 011: FastEndpoints vs Minimal API Performance Comparison

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: The dual API architecture requires validation that both approaches meet performance requirements.

## Context
The AMSA API implements both FastEndpoints and Minimal API approaches (ADR 001). Performance characteristics differ between the two, requiring systematic comparison to guide technology choices and optimization efforts.

## Decision
Maintain both approaches with documented performance characteristics. FastEndpoints for complex endpoints with type safety, Minimal API for simpler endpoints with raw SQL flexibility. Performance benchmarks guide optimization priorities.

## Alternatives Considered
- **Alternative 1: Standardize on FastEndpoints**: Pros: Consistent, type-safe. Cons: May sacrifice performance for simple endpoints.
- **Alternative 2: Standardize on Minimal API**: Pros: Flexible, performant. Cons: Less structure for complex endpoints.
- **Alternative 3: Choose per endpoint**: Pros: Optimal per case. Cons: Complex maintenance.

Dual approach with performance guidance chosen.

## Benchmark Setup
Reference: `MemberFastEndpointsBenchmark.cs`, `ApiPerformanceBenchmark.cs`.

- Environment: .NET 8, Kestrel server.
- Test scenarios: Member CRUD, search, role aggregation, statistics.
- Load testing: 100 concurrent requests, realistic data sets.
- Metrics: Response time, throughput, memory, CPU.

Benchmark methodology: In-process TestServer with HttpClient for HTTP-level comparison using ApiComparisonBenchmark.cs, measuring end-to-end request performance.

## Performance Metrics and Results
Comprehensive comparison results:
```
API Performance Comparison (100 concurrent requests)

| Scenario | FastEndpoints | Minimal API | Winner | Margin |
|----------|---------------|-------------|--------|--------|
| Simple Get| 45ms         | 42ms       | Minimal| 7%     |
| Complex Get| 85ms        | 78ms       | Minimal| 8%     |
| Search    | 120ms        | 95ms       | Minimal| 21%    |
| Statistics| 150ms        | 135ms      | Minimal| 10%    |
| Create    | 65ms         | 62ms       | Minimal| 5%     |

Memory Usage (per request):
| Scenario | FastEndpoints | Minimal API | Winner | Margin |
|----------|---------------|-------------|--------|--------|
| Simple Get| 2.1 MB       | 1.8 MB     | Minimal| 14%    |
| Complex Get| 4.2 MB       | 3.8 MB     | Minimal| 10%    |
```

Full report: [ApiComparisonBenchmark.html](Benchmarks/reports/ApiComparisonBenchmark.html)

## Performance Tradeoffs
- Gains: Minimal API generally 5-20% faster, lower memory usage.
- Costs: FastEndpoints provides better type safety and structure.
- Risks: Performance differences may change with .NET updates.

## Monitoring and Alerting
- Track performance per API approach.
- Alert if FastEndpoints exceeds Minimal API by >50%.

## Regression Prevention
- Regular performance comparisons in CI/CD.
- Benchmark thresholds for both approaches.

## Consequences
- Positive: Data-driven technology choices, optimal performance.
- Negative: Complexity of maintaining two approaches.
- Neutral: Both meet performance requirements.

## Implementation Notes
- FastEndpoints: Better for complex validation and documentation.
- Minimal API: Better for raw performance and SQL control.
- Benchmarks guide optimization priorities.

## Related ADRs
- ADR 001: Dual API Architecture (performance validation).
- ADR 005-010: All performance optimizations apply to both approaches.