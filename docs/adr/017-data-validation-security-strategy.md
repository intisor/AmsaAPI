# ADR 017: Data Validation Security Strategy

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: API inputs require comprehensive validation to prevent security vulnerabilities and data corruption.

## Security Context
The AMSA API accepts user input via POST/PUT requests for member creation/updates, CSV imports, and search parameters. Insufficient validation could lead to injection attacks, data corruption, or denial of service.

## Threat Model
- Injection via unvalidated inputs (SQL, XSS).
- Data corruption through invalid formats.
- Denial of service via oversized inputs.
- Business logic bypass through malformed data.

## Security Requirements
- Automatic model validation with DataAnnotations.
- Manual business rule validation (MKAN uniqueness, foreign key existence).
- Input sanitization and normalization.
- Size and format limits on all inputs.

## Alternatives Considered
- **Alternative 1: Manual validation only**: Pros: Flexible. Cons: Error-prone, inconsistent.
- **Alternative 2: FluentValidation**: Pros: Rich validation. Cons: Additional dependency, complexity.
- **Alternative 3: No validation**: Pros: Simple. Cons: Security nightmare.

DataAnnotations + manual checks chosen for balance.

## Security Implications
- Prevents injection through type and format validation.
- Ensures data integrity and consistency.
- Provides consistent error responses.

## Compliance Considerations
- OWASP: Input validation and sanitization.
- GDPR: Data quality and accuracy.

## Security Testing Requirements
- Unit tests for all validation rules.
- Integration tests for error responses.
- Fuzz testing for edge cases.

## Security Monitoring
- Log validation failures.
- Alert on unusual validation error patterns.
- Monitor input sizes and frequencies.

## Consequences
- Positive: Secure, consistent validation.
- Negative: Additional validation code.
- Neutral: Automatic model binding integration.

## Implementation Notes
- DataAnnotations in DTOs: [Required], [MaxLength], [EmailAddress].
- Manual checks: MKAN uniqueness, foreign key validation.
- Error responses: ProblemDetails with validation errors.
- CSV import: Extension/size validation, row-level checks.

## Related ADRs
- ADR 015: CSV Import/Export (file validation).
- ADR 012: Error Handling (validation error responses).