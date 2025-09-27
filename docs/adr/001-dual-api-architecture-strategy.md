# ADR 001: Dual API Architecture Strategy

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: The application needs to support both modern FastEndpoints and traditional Minimal API approaches to provide flexibility for different development scenarios and team preferences.

## Context
The AMSA Nigeria API serves multiple stakeholders with varying technical requirements. Some endpoints need high performance and type safety (FastEndpoints), while others benefit from the simplicity and flexibility of Minimal API. The existing codebase shows both approaches implemented side by side, with FastEndpoints in `/FastEndpoints/` directory and Minimal API in `/Endpoints/` directory. Program.cs configures both API styles, and API_VERSIONING_GUIDE.md provides guidance on when to use each approach.

## Decision
Implement a dual API architecture with both FastEndpoints and Minimal API approaches running simultaneously. FastEndpoints handles complex endpoints with strong typing and validation, while Minimal API handles simpler endpoints and provides flexibility for custom implementations. Both APIs share the same database context and DTOs, ensuring consistency.

## Alternatives Considered
- **Alternative 1: FastEndpoints Only**: Pros: Consistent, type-safe, modern. Cons: Steeper learning curve, less flexibility for custom logic.
- **Alternative 2: Minimal API Only**: Pros: Simple, flexible, familiar to .NET developers. Cons: Less structure, harder to maintain at scale.
- **Alternative 3: Controller-based MVC**: Pros: Traditional, well-known. Cons: Verbose, less performant, not suitable for microservices.

Dual approach chosen for maximum flexibility while maintaining consistency.

## Consequences
- **Positive**: Supports different development styles, allows gradual migration, provides options for performance vs simplicity tradeoffs.
- **Negative**: Code duplication potential, complexity in maintaining two approaches, potential confusion for new developers.
- **Neutral**: Both APIs share common infrastructure (DbContext, DTOs), minimal overhead.

## Implementation Notes
- FastEndpoints: `/api/*` routes, strong typing, built-in validation.
- Minimal API: `/api/minimal/*` routes, direct request handling.
- Shared: DTOs in `/DTOs/`, DbContext in `/Data/`, extensions in `/Extensions/`.
- Configuration in Program.cs with separate builder methods.

## Related ADRs
- ADR 002: Technology Stack Selection (FastEndpoints library choice).
- ADR 004: Endpoint Organization (how both approaches are organized).