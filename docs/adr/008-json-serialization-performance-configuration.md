# ADR 008: JSON Serialization Performance Configuration

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Complex object graphs with circular references require careful JSON serialization configuration.

## Context
AMSA data model has circular references: Member → Unit → State → National and navigation properties create potential serialization loops. Default JSON serialization would fail or create infinite loops without proper configuration.

## Decision
Configure `ReferenceHandler.IgnoreCycles` and ``WriteIndented = true` in `Program.cs` for both API approaches. IgnoreCycles prevents circular reference exceptions while WriteIndented improves debugging.

## Alternatives Considered
- **Alternative 1: ReferenceHandler.Preserve**: Pros: Preserves object references. Cons: Complex JSON, client-side processing overhead.
- **Alternative 2: Custom converters**: Pros: Full control. Cons: Complex implementation, maintenance burden.
- **Alternative 3: DTO flattening**: Pros: No circular references. Cons: More DTOs, duplication.

IgnoreCycles chosen for simplicity and performance.

## Benchmark Setup
Reference: `Benchmarks/ApiPerformanceBenchmark.cs`.

- Environment: .NET 8, complex object graphs.
- Test data: Members with full hierarchy navigation.
- Metrics: Serialization time, payload size, deserialization performance.

## Performance Metrics and Results
BenchmarkDotNet results:
```
| Method | Mean | Error | StdDev | Payload Size | Deserialize |
|--------|------|-------|--------|-------------|-------------|
| Ignore | 15ms | 1ms   | 2ms    | 2.1 MB      | 12ms        |
| Preserve| 45ms | 3ms   | 8ms    | 3.8 MB      | 35ms        |

WriteIndented impact:
| Method | Mean | Payload |
|--------|------|---------|
| Compact| 12ms | 1.8 MB  |
| Indented| 15ms | 2.1 MB  |
```

Full report: [SerializationBenchmark.html](Benchmarks/reports/SerializationBenchmark.html)

## Performance Tradeoffs
- Gains: Prevents serialization failures, readable JSON.
- Costs: Slightly larger payloads, minor speed impact.
- Risks: Silent data omission in circular references.

## Monitoring and Alerting
- Monitor response sizes and serialization times.
- Alert on serialization errors.

## Regression Prevention
- Unit tests for serialization of complex objects.
- Benchmarks include serialization performance.

## Consequences
- Positive: Reliable JSON responses, better debugging.
- Negative: Potential data loss in circular references.
- Neutral: Consistent across both API approaches.

## Implementation Notes
- `Program.cs`: `ReferenceHandler.IgnoreCycles`, `WriteIndented = true`.
- Affects both FastEndpoints and Minimal API responses.
- Circular references handled gracefully.

## Related ADRs
- ADR 003: Database Design (navigation properties create circular refs).
- ADR 011: API Performance Comparison (serialization affects both APIs).