# ADR 004: Endpoint Organization - Separation of Concerns

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Organizing API endpoints to maximize maintainability and team productivity while supporting both FastEndpoints and Minimal API approaches.

## Context
The API has multiple endpoint categories (Members, Organizations, Statistics, Imports, Departments) with both FastEndpoints and Minimal API implementations. The codebase needs clear organization to prevent confusion and enable parallel development by multiple team members.

## Decision
Organize endpoints into feature-based files (MemberEndpoints.cs, OrganizationEndpoints.cs, etc.) rather than individual endpoint files. Group related operations together and use consistent naming patterns. Both FastEndpoints and Minimal API follow the same organizational structure.

## Alternatives Considered
- **Alternative 1: Individual endpoint files**: Pros: Clear separation, easy to find specific endpoints. Cons: Too many files, harder to see related operations, more boilerplate.
- **Alternative 2: Controller-based organization**: Pros: Traditional MVC structure. Cons: Not suitable for Minimal API, more verbose than needed.
- **Alternative 3: Single large file per API type**: Pros: Simple. Cons: Hard to maintain, merge conflicts, poor discoverability.

Feature-based organization chosen for balance of clarity and maintainability.

## Consequences
- **Positive**: Easy to find related operations, reduces file count, supports team development, consistent patterns.
- **Negative**: Larger files may require more scrolling, potential for unrelated code in same file.
- **Neutral**: Both API approaches benefit equally, extension methods handle registration cleanly.

## Implementation Notes
- /Endpoints/ directory: Minimal API implementations.
- /FastEndpoints/ directory: FastEndpoints implementations.
- Each file contains related endpoints (CRUD operations for a domain).
- Program.cs uses extension methods for clean registration.
- Error handling patterns are consistent within each approach (Minimal APIs use explicit try-catch; FastEndpoints rely on framework patterns/filters).

## Related ADRs
- ADR 001: Dual API Architecture (how organization supports both approaches).
- ADR 003: Database Design (how endpoint organization handles schema complexity).