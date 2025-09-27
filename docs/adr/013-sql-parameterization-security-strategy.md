# ADR 013: SQL Parameterization Security Strategy

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Raw SQL queries in the API must be protected against SQL injection attacks.

## Security Context
The AMSA API uses raw SQL queries in `OrganizationEndpoints.cs` and `StatisticsEndpoints.cs` for performance-critical operations. User input from API parameters could potentially be injected into SQL if not properly parameterized.

## Threat Model
- SQL injection via parameterized query bypass.
- Dynamic SQL construction from user input.
- Second-order injection through stored data.
- Denial of service via malformed queries.

## Security Requirements
- All raw SQL queries must use parameterized execution.
- No string concatenation of user input into SQL.
- Use EF Core's `SqlQueryRaw<T>(sql, args...)` with `{0}` placeholders.
- Validate and sanitize input parameters.

## Alternatives Considered
- **Alternative 1: Avoid raw SQL entirely**: Pros: No injection risk. Cons: Performance limitations for complex queries.
- **Alternative 2: Stored procedures**: Pros: Pre-compiled, parameterized. Cons: Database-specific, harder to maintain.
- **Alternative 3: Manual escaping**: Pros: Simple. Cons: Error-prone, incomplete protection.

EF Core parameterization chosen for security and performance.

## Security Implications
- Prevents SQL injection attacks on raw SQL queries.
- Maintains performance benefits of raw SQL.
- Consistent with EF LINQ parameterization.

## Compliance Considerations
- OWASP Top 10: A03 Injection prevention.
- PCI DSS: Secure coding practices.
- GDPR: Data protection through secure queries.

## Security Testing Requirements
- Unit tests verify parameterization with malicious input.
- Static analysis for SQL injection patterns.
- Dynamic security testing with SQLMap.

## Security Monitoring
- Log all raw SQL executions with parameters.
- Alert on unusual query patterns.
- Monitor for SQL error patterns in logs.

## Consequences
- Positive: Secure raw SQL usage, performance maintained.
- Negative: Additional parameter management.
- Neutral: Consistent with EF LINQ security.

## Implementation Notes
- `OrganizationEndpoints.cs`: `GetUnitById` uses `SqlQueryRaw<Unit>("SELECT * FROM Units WHERE UnitId = {0}", id)`.
- `GetUnitsByState`: `SqlQueryRaw<Unit>("SELECT * FROM Units WHERE StateId = {0}", stateId)`.
- `StatisticsEndpoints.cs`: Aggregation queries use parameterized counts and joins.
- All user inputs passed as parameters, never concatenated.

## Related ADRs
- ADR 005: Raw SQL vs EF LINQ (security for performance queries).
- ADR 016: Security Review Raw SQL Usage (comprehensive review).