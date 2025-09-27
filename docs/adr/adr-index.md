# ADR Index

This index catalogs all Architecture Decision Records. It is manually maintained but can be updated using `update-index.ps1`.

## ADR Catalog

| # | Title | Status | Date | Description | Category |
|---|-------|--------|------|-------------|----------|
| 001 | Dual API Architecture Strategy | Accepted | 2025-09-27 | Decision to implement both FastEndpoints and Minimal API approaches | Architecture |
| 002 | Technology Stack Selection | Accepted | 2025-09-27 | Choices of .NET 8, EF Core, SQL Server, and supporting libraries | Technology |
| 003 | Database Design - Normalized Schema | Accepted | 2025-09-27 | Hierarchical schema design with normalized tables and relationships | Database |
| 004 | Endpoint Organization - Separation of Concerns | Accepted | 2025-09-27 | Feature-based endpoint grouping strategy | Organization |
| 005 | Raw SQL vs EF LINQ Performance Strategy | Accepted | 2025-09-27 | Strategic use of raw SQL for complex queries vs EF LINQ for CRUD | Performance |
| 006 | AsNoTracking Strategy Implementation | Accepted | 2025-09-27 | Consistent AsNoTracking usage for read operations | Performance |
| 007 | DTO Mapping Performance Strategy | Accepted | 2025-09-27 | Extension method mapping and role aggregation optimization | Performance |
| 008 | JSON Serialization Performance Configuration | Accepted | 2025-09-27 | ReferenceHandler.IgnoreCycles and serialization settings | Performance |
| 009 | Search Strategy Performance Comparison | Accepted | 2025-09-27 | Contains vs EF.Functions.Like performance differences | Performance |
| 010 | Benchmark Infrastructure Integration | Accepted | 2025-09-27 | BenchmarkDotNet setup and integration | Performance |
| 011 | FastEndpoints vs Minimal API Performance Comparison | Accepted | 2025-09-27 | Performance comparison between dual API approaches | Performance |
| 012 | Error Handling Strategy - Try/Catch vs Middleware | Accepted | 2025-09-27 | Explicit try/catch + Results.Problem pattern vs middleware | Security |
| 013 | SQL Parameterization Security Strategy | Accepted | 2025-09-27 | Use of SqlQueryRaw with parameters to prevent injection | Security |
| 014 | Search Strategy - Collation & LIKE Patterns | Accepted | 2025-09-27 | EF.Functions.Like with SQL_Latin1_General_CP1_CI_AI collation | Security |
| 015 | CSV Import/Export Strategy | Accepted | 2025-09-27 | Secure CSV handling with CsvHelper and validation | Operations |
| 016 | Security Review - Raw SQL Usage | Accepted | 2025-09-27 | Comprehensive review of raw SQL for injection risks | Security |
| 017 | Data Validation Security Strategy | Accepted | 2025-09-27 | DataAnnotations and manual validation for secure inputs | Security |
| 000 | Example Standard ADR | Accepted | 2025-09-27 | Example of a standard architectural decision | Architecture |
| 000 | Example Performance ADR | Accepted | 2025-09-27 | Example of a performance-related decision with benchmarks | Performance |

## Categories
- **Architecture**: System design, framework choices.
- **Technology**: Stack decisions, library selections.
- **Database**: Schema, queries, migrations.
- **Organization**: Code structure, endpoint grouping.
- **Performance**: Optimization strategies, benchmarks.
- **Security**: Authentication, injection prevention, validation.
- **Operations**: Import/export, file handling, deployment.
