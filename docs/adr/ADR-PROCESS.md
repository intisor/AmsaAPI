# ADR Process Guidelines

## When to Create an ADR
Create an ADR for significant decisions:
- Architectural patterns (e.g., layered architecture in `ARCHITECTURE.md`).
- Technology choices (e.g., FastEndpoints vs traditional controllers).
- Performance optimizations (e.g., EF Core query strategies).
- Security implementations (e.g., auth in API endpoints).
- Database schema changes or query patterns.
- API design and versioning (see `API_VERSIONING_GUIDE.md`).

Avoid for minor changes like bug fixes or simple refactors.

## ADR Lifecycle
1. **Proposed**: Draft created via `new-adr.ps1`, discussed in team meetings.
2. **Under Review**: Shared for feedback, iterate as needed.
3. **Accepted/Rejected**: Approved via PR review or meeting consensus. Update status with `adr-status.ps1`.
4. **Implemented**: Code changes merged, reference ADR in commits.
5. **Deprecated/Superseded**: When decisions evolve, create a new ADR linking back.

## Review Process
- Submit ADR as a draft PR.
- Tag relevant team members.
- Require at least one approval from a senior architect.
- For performance ADRs, include benchmark results before approval.
- Merge only after status is "Accepted".

## Integration with Workflow
- Link ADRs in PR descriptions for context.
- Reference ADRs in code comments for key decisions (e.g., hierarchy endpoint optimization).
- Update `README.md` or `ARCHITECTURE.md` to point to relevant ADRs.
- For performance: Run benchmarks during PRs using existing BenchmarkDotNet setup.

## Performance Benchmark Requirements
Performance ADRs must:
- Use `BenchmarkRunner.cs` for reproducible tests.
- Compare before/after metrics.
- Discuss sustainability (e.g., monitoring in appsettings.json).

## Configuration
ADR tools use a configuration file `.adr-tools.json` (or `.adr-tools`) at the repository root for settings. CLI parameters override config values, which override defaults.

### Config Keys
- `adrDirectory`: Path to ADR folder (default: "docs/adr")
- `defaultTemplate`: Default ADR type (default: "standard")
- `editor`: Editor command to open ADRs (default: "code")
- `numberingFormat`: Number padding format (default: "D3")
- `templates`: Object mapping types to template paths
- `indexFile`: Index file name (default: "adr-index.md")
- `statusTransitions`: Object defining allowed status changes

### Precedence
CLI args > config > defaults. If config is missing, defaults are used.

## Examples
See `/docs/adr/examples/` for good practices:
- Good: Clear rationale, quantifiable tradeoffs.
- Poor: Vague context, no alternatives.

Maintain consistency with existing docs like `ARCHITECTURE.md` (detailed diagrams) and `API_VERSIONING_GUIDE.md` (step-by-step guides).
