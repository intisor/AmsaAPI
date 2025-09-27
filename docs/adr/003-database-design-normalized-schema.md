# ADR 003: Database Design - Normalized Schema

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Designing a database schema that accurately represents the hierarchical organizational structure of AMSA Nigeria while maintaining data integrity and query performance.

## Context
AMSA Nigeria has a complex hierarchical structure: National level contains States, States contain Units, Units contain Members. Members can hold multiple roles through departments at different levels. The schema must support this hierarchy while enabling efficient queries for statistics, member management, and role assignments.

## Decision
Implement a fully normalized schema with separate tables for each entity level (National, State, Unit, Member, Department, Level) and junction tables for many-to-many relationships (MemberLevelDepartments, LevelDepartments). Use foreign key constraints and unique indexes to maintain referential integrity.

## Alternatives Considered
- **Alternative 1: Denormalized schema**: Pros: Simpler queries, better read performance. Cons: Data redundancy, update anomalies, harder to maintain hierarchy changes.
- **Alternative 2: Document database (MongoDB)**: Pros: Flexible hierarchy representation. Cons: Less relational integrity, harder SQL-based reporting, different tooling.
- **Alternative 3: Single table with hierarchy columns**: Pros: Simple structure. Cons: Limited scalability, complex queries, data integrity issues.

Normalized schema chosen for data integrity, flexibility, and relational power.

## Consequences
- **Positive**: Strong data integrity, flexible querying, supports complex relationships, easy to extend.
- **Negative**: More complex queries for hierarchical data, requires JOINs for common operations.
- **Neutral**: EF Core handles complexity well, migrations provide schema evolution.

## Implementation Notes
- AmsaDbContext.cs defines all entities and relationships.
- Migration files track schema evolution.
- Unique constraints prevent duplicate assignments.
- Foreign keys enforce referential integrity.
- Indexes on commonly queried columns (e.g., MemberId, UnitId).

## Related ADRs
- ADR 002: Technology Stack (EF Core integration).
- ADR 004: Endpoint Organization (how schema complexity is handled in endpoints).