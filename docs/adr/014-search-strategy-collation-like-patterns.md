# ADR 014: Search Strategy - Collation & LIKE Patterns

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Member search functionality requires case-insensitive, accent-insensitive searching across names.

## Security Context
Search functionality in `MemberEndpoints.cs` uses `EF.Functions.Like()` with specific collation. Incorrect collation or pattern usage could lead to security issues or performance problems.

## Threat Model
- Denial of service via expensive wildcard searches.
- Information disclosure through pattern matching bypass.
- Performance degradation from non-indexed searches.

## Security Requirements
- Use parameterized LIKE patterns to prevent injection.
- Apply appropriate collation for case/accent insensitivity.
- Limit search scope and results to prevent DoS.
- Validate search input length and patterns.

## Alternatives Considered
- **Alternative 1: Application-level normalization**: Pros: Portable. Cons: Loads all data to memory.
- **Alternative 2: Full-text search**: Pros: Advanced features. Cons: Complex setup, SQL Server specific.
- **Alternative 3: Different collations per locale**: Pros: Internationalization. Cons: Complexity, performance.

Fixed collation chosen for simplicity and performance.

## Security Implications
- Parameterized LIKE prevents injection.
- Collation ensures consistent matching.
- Wildcard limits prevent regex-like attacks.

## Compliance Considerations
- OWASP: Input validation and parameterized queries.
- Accessibility: Consistent search behavior.

## Security Testing Requirements
- Test with malicious LIKE patterns (% , _ wildcards).
- Verify collation consistency across environments.
- Performance testing with large datasets.

## Security Monitoring
- Log search queries and patterns.
- Alert on unusual search volumes or patterns.
- Monitor query execution times.

## Consequences
- Positive: Secure, performant searching.
- Negative: Collation dependency.
- Neutral: Consistent user experience.

## Implementation Notes
- `MemberEndpoints.cs`: `EF.Functions.Like(name, $"%{searchTerm}%").Collate("SQL_Latin1_General_CP1_CI_AI")`.
- Case/accent insensitive via collation.
- Indexing: Consider indexes on FirstName, LastName.
- Prefix-only search for typeahead: No leading wildcard.

## Related ADRs
- ADR 009: Search Strategy Performance (collation performance).
- ADR 017: Data Validation (search input validation).