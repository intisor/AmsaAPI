# ADR 010: Benchmark Infrastructure Integration

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Systematic performance measurement is essential for maintaining API performance in dual architecture.

## Context
The AMSA API has complex performance requirements with hierarchical data, dual API implementations, and various optimization strategies. Without systematic benchmarking, performance regressions and optimization opportunities would be missed.

## Decision
Implement comprehensive BenchmarkDotNet infrastructure with multiple benchmark classes, educational output, and CI/CD integration. Use realistic test data and measure key performance metrics for all major operations.

## Alternatives Considered
- **Alternative 1: Manual timing**: Pros: Simple. Cons: Inconsistent, no statistical analysis.
- **Alternative 2: Application Insights only**: Pros: Production monitoring. Cons: No development-time feedback.
- **Alternative 3: Custom benchmarking**: Pros: Tailored. Cons: Maintenance overhead.

BenchmarkDotNet chosen for industry standard, comprehensive features.

## Benchmark Setup
Reference: `Benchmarks/BenchmarkRunner.cs`, `ApiPerformanceBenchmark.cs`, `MemberFastEndpointsBenchmark.cs`.

- Environment: .NET 8, isolated benchmarking.
- Test data: Hierarchical structures (5 nationals, 50 states, 500 units, 10,000 members).
- Categories: Data transformation, search algorithms, role aggregation, parallel processing.
- Diagnosers: Memory, CPU usage, GC statistics.

## Performance Metrics and Results
Benchmark categories established:
- **Data Transformation**: Entity → DTO mapping performance.
- **Search Algorithms**: Contains vs Like vs IndexOf comparisons.
- **Role Aggregation**: Linq vs GroupBy vs ToLookup strategies.
- **Parallel Processing**: Multi-threading optimizations.

Sample results structure:
```
BenchmarkDotNet=v0.15.3
OS=Windows 11
CPU=Intel Core i7-9750H, 1 CPU, 12 logical cores
.NET SDK=8.0.100
  [Host]     : .NET 8.0.0, X64 RyuJIT AVX2
  [ShortRun] : .NET 8.0.0, X64 RyuJIT AVX2

| Method | Mean | Error | StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------|------|-------|--------|-------|-------|-------|-----------|
```

## Performance Tradeoffs
- Gains: Data-driven performance decisions, regression detection.
- Costs: Development time for benchmark creation.
- Risks: Benchmarks may not reflect production exactly.

## Monitoring and Alerting
- CI/CD runs benchmarks on PRs.
- Performance regression alerts.
- Historical performance tracking.

## Regression Prevention
- Benchmarks run automatically in CI/CD.
- Performance gates prevent regressions.
- Regular benchmark reviews.

## Consequences
- Positive: Systematic performance optimization, data-driven decisions.
- Negative: Initial setup time, ongoing maintenance.
- Neutral: Supports both development and production monitoring.

## Implementation Notes
- `BenchmarkRunner.cs`: Educational output with explanations.
- `Program.cs`: Command-line integration (`args[0] == "benchmark"`).
- Multiple benchmark classes for different concerns.
- Realistic test data generation.

## Related ADRs
- ADR 011: API Performance Comparison (uses benchmark infrastructure).
- ADR 005-009: All performance ADRs reference benchmark results.