# ADR 015: CSV Import/Export Strategy

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: Bulk member import from CSV files requires secure file handling and data validation.

## Context
The AMSA API supports CSV import for EXCO members via `ImportEndpoints.cs`. Files are uploaded, validated, processed with CsvHelper, and unmatched records reported. Export functionality is planned for data extraction.

## Decision
Use CsvHelper for both import and export with strict validation. Import: Extension check (.csv), size limit (10MB), temp file handling, unmatched record logging. Export: Streaming responses, RFC 4180 compliance, batching for large datasets.

## Alternatives Considered
- **Alternative 1: Manual CSV parsing**: Pros: Lightweight. Cons: Error-prone, limited features.
- **Alternative 2: Excel format**: Pros: Richer data. Cons: Larger files, library dependencies.
- **Alternative 3: JSON import**: Pros: Structured. Cons: Not standard for bulk data.

CsvHelper chosen for reliability and standards compliance.

## Consequences
- **Positive**: Secure file handling, robust parsing, scalable export.
- **Negative**: Library dependency, file size limits.
- **Neutral**: Consistent with industry standards.

## Implementation Notes
- Import: `ImportEndpoints.cs` validates extension/size, uses temp files, CsvHelper reads with mapping.
- `ExcoImporter.cs`: Matches by name/MKAN, logs unmatched records.
- Export (planned): Streaming `FileStreamResult` with CsvHelper writer, column mapping.

## Data Normalization & Matching
- Trim whitespace, normalize Unicode, remove diacritics.
- Prefer unique IDs; fallback to Levenshtein/s Soundex matching (with review).
- Log unmatched with structured fields for triage.

## Export Strategy (Deferred)
- Use CsvHelper for streaming export.
- Content-Type: text/csv, RFC 4180 adherence.
- Batching for large datasets, configurable columns.
- Acceptance criteria: Export 10k+ records without OOM.

## Related ADRs
- ADR 017: Data Validation (CSV input validation).
- ADR 013: SQL Parameterization (secure bulk inserts).