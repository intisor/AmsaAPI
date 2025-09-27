# ADR 012: Error Handling Strategy - Try/Catch vs Middleware

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: The API needs consistent error handling across both FastEndpoints and Minimal API implementations while providing appropriate HTTP status codes and problem details.

## Context
The AMSA API handles various error scenarios: validation failures, not found resources, database constraints, file upload issues, and unexpected exceptions. Both API approaches need consistent error responses following RFC 7807 Problem Details format.

## Decision
Use explicit try/catch blocks with typed Results (Results.Problem, Results.NotFound, Results.BadRequest) for business logic errors in endpoint methods. Reserve global middleware for unexpected exceptions and cross-cutting concerns like logging and correlation IDs.

## Alternatives Considered
- **Alternative 1: Global middleware only**: Pros: Centralized, consistent. Cons: Harder to provide context-specific error details, less granular control.
- **Alternative 2: No explicit handling**: Pros: Simple. Cons: Generic 500 errors, poor user experience.
- **Alternative 3: Custom exception types**: Pros: Type-safe. Cons: Complex hierarchy, more code.

Explicit try/catch chosen for granular control and clear error boundaries.

## Consequences
- **Positive**: Clear error boundaries, appropriate HTTP codes, detailed problem responses.
- **Negative**: Some code duplication in error patterns.
- **Neutral**: Middleware handles logging and correlation IDs consistently.

## Implementation Notes
- Explicit try/catch in `MemberEndpoints.cs`: `GetMemberById` returns Results.NotFound if member not found.
- `OrganizationEndpoints.cs`: `CreateUnit` uses Results.BadRequest for validation failures.
- `StatisticsEndpoints.cs`: Database errors return Results.Problem with details.
- `ImportEndpoints.cs`: File upload errors use Results.Problem with specific messages.
- `Program.cs`: Global `UseExceptionHandler` for unhandled exceptions.
- All errors include correlation ID and ProblemDetails structure.

## Related ADRs
- ADR 001: Dual API Architecture (consistent across both approaches).
- ADR 017: Data Validation (validation errors handled explicitly).