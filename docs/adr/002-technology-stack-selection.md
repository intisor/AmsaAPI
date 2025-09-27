# ADR 002: Technology Stack Selection

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Choosing the right technology stack for a .NET API serving organizational data with complex hierarchies and performance requirements.

## Context
The AMSA Nigeria API needs to handle hierarchical organizational data (National → State → Unit → Member) with role-based permissions, CSV imports, and statistical reporting. The stack must support both FastEndpoints and Minimal API approaches, provide excellent EF Core performance, and enable benchmarking for performance decisions.

## Decision
Adopt .NET 8 LTS, Entity Framework Core 8.0.8, SQL Server, FastEndpoints 7.1.0-beta.11, BenchmarkDotNet 0.15.3, and CsvHelper 33.1.0. This combination provides modern performance, type safety, and flexibility for the dual API architecture.

## Alternatives Considered
- **Alternative 1: .NET 6 LTS**: Pros: Stable, widely adopted. Cons: Missing .NET 8 performance improvements, shorter support timeline.
- **Alternative 2: Dapper instead of EF Core**: Pros: Better raw performance. Cons: More boilerplate, less LINQ support, harder for complex queries.
- **Alternative 3: PostgreSQL instead of SQL Server**: Pros: Open source, good JSON support. Cons: Less enterprise adoption, different tooling ecosystem.
- **Alternative 4: Minimal API without FastEndpoints**: Pros: Simpler stack. Cons: Less structure for complex endpoints.

Current stack chosen for balance of performance, productivity, and enterprise readiness.

## Consequences
- **Positive**: Excellent performance, strong typing, rich ecosystem, future-proof with LTS support.
- **Negative**: Beta dependency on FastEndpoints, learning curve for new team members.
- **Neutral**: SQL Server provides robust enterprise features, BenchmarkDotNet enables data-driven performance decisions.

## Implementation Notes
- AmsaAPI.csproj defines all package versions and references.
- Program.cs configures EF Core with SQL Server connection.
- BenchmarkDotNet integrated for performance ADRs.
- CsvHelper used in ExcoImporter.cs for bulk operations.
- ADRs reflect the csproj's declared versions and will be revised when dependencies are upgraded.

## Related ADRs
- ADR 001: Dual API Architecture (how technologies support both approaches).
- ADR 003: Database Design (EF Core integration with SQL Server).