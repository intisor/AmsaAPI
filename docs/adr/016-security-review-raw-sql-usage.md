# ADR 016: Security Review - Raw SQL Usage

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Comprehensive security review of all raw SQL queries to ensure parameterization and injection protection.

## Security Context
Raw SQL queries in `OrganizationEndpoints.cs` and `StatisticsEndpoints.cs` provide performance benefits but introduce injection risks if not properly parameterized.

## Threat Model
- SQL injection via unparameterized queries.
- Privilege escalation through dynamic SQL.
- Data exposure via query manipulation.
- Denial of service via malformed queries.

## Security Requirements
- All raw SQL must use EF Core parameterization.
- No user input concatenation into SQL strings.
- Minimal database privileges for the application.
- Query whitelisting for dynamic elements.

## Alternatives Considered
- **Alternative 1: Eliminate raw SQL**: Pros: No injection risk. Cons: Performance impact.
- **Alternative 2: ORM-only**: Pros: Automatic protection. Cons: Complex query limitations.
- **Alternative 3: Manual review only**: Pros: Simple. Cons: Human error prone.

Systematic review with checklist chosen.

## Security Implications
- Eliminates injection vectors in performance-critical queries.
- Establishes security baseline for raw SQL usage.
- Enables safe expansion of raw SQL where needed.

## Compliance Considerations
- OWASP Top 10: A03 Injection.
- ISO 27001: Secure coding practices.
- PCI DSS: Database security controls.

## Security Testing Requirements
- Static analysis for SQL injection patterns.
- Dynamic testing with SQL fuzzing tools.
- Code review checklist enforcement.
- Unit tests for parameterized queries.

## Security Monitoring
- Log all raw SQL executions.
- Alert on SQL errors or unusual patterns.
- Database audit logging enabled.

## Consequences
- Positive: Secure high-performance queries.
- Negative: Additional review overhead.
- Neutral: Maintains performance benefits.

## Implementation Notes
- Inventory: `OrganizationEndpoints.cs` (GetUnitById, GetUnitsByState, etc.), `StatisticsEndpoints.cs` (aggregations).
- All queries confirmed parameterized with SqlQueryRaw<T>(sql, args...).
- No string concatenation found.
- Checklist: Parameters only, no dynamic identifiers, enums/whitelists, minimal privileges.

## Review Checklist
- [x] All queries use parameterization (SqlQueryRaw with args).
- [x] No user input string concatenation.
- [x] Dynamic elements use whitelists/enums.
- [x] Minimal database privileges.
- [x] Logging and error handling in place.
- [x] Unit tests verify parameterization.

## Related ADRs
- ADR 013: SQL Parameterization (parameterization strategy).
- ADR 005: Raw SQL vs EF LINQ (security for performance).