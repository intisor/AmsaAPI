# AmsaAPI Performance Rework Requirements

## 1. Purpose and portfolio role

AmsaAPI is the portfolio's organization and identity API. It owns members, units, states, national hierarchy, departments/roles, application registrations, JWT issuance, CSV import, and statistics consumed by AMSA Reporting System and other apps. The rework must turn its current performance-oriented experiments into a reproducible, secure, observable API case study.

The rework is needed because the runtime exposes parallel FastEndpoints and Minimal API implementations with materially different query shapes, broad anonymous access, startup migration/seeding, and no current production baseline. Historical ADRs contain benchmark-looking numbers, but the current `Benchmarks` sources are excluded from compilation, no benchmark project/package exists, the solution contains only the web project, several benchmarks use EF InMemory, and ADR setup versions differ from the current .NET 10 application. Those numbers are not a verified current baseline and must not be reused as results.

## 2. Verified current baseline

Verified facts, not performance claims:

- `AmsaAPI.csproj` targets `net10.0`, references FastEndpoints 8.1, EF Core SQL Server/InMemory 10.0.1, JWT bearer, CSVHelper, MVC Testing, and a Visual Studio diagnostics BenchmarkDotNet diagnoser package. It does **not** reference the BenchmarkDotNet package.
- The project explicitly removes `Benchmarks/**/*.cs` from compilation. `AmsaAPI.sln` includes only `AmsaAPI.csproj`; there is no benchmark project. Therefore the existing benchmark code is orphaned from normal build/run and cannot satisfy ADR 010's claimed CI integration.
- `Benchmarks/BenchmarkRunner.cs` references BenchmarkDotNet, waits for key input, and runs seven benchmark classes, but `Program.cs` has no benchmark command-line path. Existing comments/ADR instructions are inconsistent with the current runtime.
- Existing benchmark classes mix pure in-memory transformations, EF InMemory queries, and in-process `WebApplicationFactory` requests. Their fixed fixture sizes are useful ideas but not SQL Server/Kestrel capacity evidence.
- ADRs 005/010/011 document historical environments and results (including .NET 8 and specific latency/memory figures), but those claims were not verified against current source/runtime during this requirements review. Treat them as hypotheses/history only.
- `Program.cs` configures SQL Server with retry-on-failure, FastEndpoints, JWT authentication/authorization, static/Razor pages, and both endpoint families. It runs migrations and seeds a ReportingApp registration at web startup.
- The seed contains a placeholder `AppSecretHash` string in application code. Token generation accepts `appSecret` at the contract layer, but the inspected `TokenService.GenerateTokenAsync` does not validate an app secret before issuing a member token.
- Many FastEndpoints call `AllowAnonymous()`, including member and statistics routes. Minimal API groups shown in source do not require authorization. Authentication middleware exists, but endpoint authorization/scopes are not consistently enforced.
- FastEndpoints member “get all” loads a full entity graph for all members and maps in memory; no paging is present. Member-by-MKAN uses one member query plus one roles query.
- Minimal member endpoints use raw SQL, materialize flat joined rows, then group/map in memory. Search uses a leading/trailing wildcard and explicit collation.
- FastEndpoints dashboard statistics issues multiple independent count queries; Minimal dashboard uses one count statement plus a recent-members query. Organization summary similarly differs between implementations.
- Statistics and organization endpoints contain hand-written SQL aggregations. Existing schema has unique indexes for member phone/MKAN/email and department/state/national names, but model configuration does not explicitly show all foreign-key/aggregate-support indexes needed by joins.
- CSV import reads all records and most reference/member/assignment tables into memory. It calls `SaveChangesAsync` and reloads a member inside the per-record loop for newly created members; final assignments are batched.
- `TokenService` loads registrations/members/roles, creates JWTs, updates `LastUsedAt`, and saves per issuance. No token rate limiting or cache is configured in inspected startup.
- Broad `catch (Exception)` handlers return exception messages in problem responses, risking information disclosure and unstable error contracts.
- Integration tests use `WebApplicationFactory` with EF InMemory despite a comment mentioning SQLite. Tests cover authentication and endpoint families, but EF InMemory cannot validate raw SQL or SQL Server behavior.
- No OpenTelemetry, k6/NBomber, Dockerfile, Aspire host, or active GitHub Actions workflow was found in the inspected source inventory.

## 3. Baseline and target outcomes

### Baseline first

Create deterministic SQL Server datasets for small/medium/large organization shapes: nationals, states, units, members, levels, departments, role assignments, and app registrations. Include skewed distributions (large units, members with multiple roles), search terms with common/rare/no matches, and import files with valid/invalid/duplicate records.

Run at least three comparable trials and record commit SHA, SDK/runtime, Kestrel/container resources, SQL Server version/tier/compatibility, connection pool, dataset cardinalities, cache state, and network topology. Capture p50/p95/p99/max, RPS, errors/timeouts, allocations/GC, CPU/memory, SQL duration/roundtrips/logical reads/plans/waits, response bytes, pool state, and auth/import-specific counters.

Run endpoint families separately and as realistic mixes. Historical ADR values must be archived as unverified until regenerated by the new harness.

### Budgets after baseline

The accepted baseline becomes `B0`; absolute budgets are ratified from consumer needs and hosting capacity and committed to `performance-budgets.json`. Until then:

- security/correctness/contract parity may not regress;
- errors may not exceed `B0` except injected failures;
- p95/p99, SQL reads/roundtrips, response bytes, and allocations may not regress beyond measured noise;
- an optimization claim must exceed run variability and retain equivalent results;
- the chosen canonical endpoint for a capability must meet or outperform the alternative within the practical threshold, or retain the alternative only for a documented non-performance benefit;
- import must be bounded in memory and transactional/idempotent according to the approved policy.

## 4. Scope and non-goals

In scope: endpoint/query architecture, canonicalization of duplicate APIs, paging/filtering, SQL plans/indexes, token/app auth, imports, serialization, caching only where justified, concurrency, microbenchmarks, Kestrel load tests, OpenTelemetry, tests, CI, containers, and optional Aspire integration with Reporting.

Non-goals: preserving both API styles merely for demonstration; replacing SQL Server or FastEndpoints without evidence; caching member/role data without invalidation/authorization analysis; inventing current performance from ADR prose; moving Reporting domain data into AmsaAPI; adding a distributed cache before a measured repeated-read need.

## 5. Required architecture rework

### 5.1 Canonical endpoint strategy

Inventory every FastEndpoints and Minimal API route by capability, contract, authorization, query count, SQL shape, result semantics, and consumers. Select one canonical route per capability. Keep a second implementation only when it provides a measured/maintainability benefit and has parity tests. Otherwise deprecate it with versioned migration guidance.

Move business/query logic out of endpoint classes into shared application query/command services so endpoint framework comparisons do not compare different algorithms. Define stable DTOs and problem details; never expose exception messages. Add paging with maximum page size to member/hierarchy/list routes and explicit lightweight summary versus detail con
tracts.

### 5.2 Query architecture

Use server-side DTO projections and `AsNoTracking`. Avoid full entity graphs for unbounded member lists. Batch/project roles and hierarchy without cartesian explosion. Compare statistics strategies (single statement, separate reads, precomputed projection) with SQL Server evidence.

Raw SQL must be parameterized, schema-change tested, and contract-equivalent to LINQ for ordering/null/count semantics. Search selection must follow product semantics and execution plans; leading-wildcard `LIKE` may need full-text/search indexing rather than a normal index.

### 5.3 Authentication and authorization

Validate app secrets with a slow hash or keyed verifier and constant-time comparison. Remove placeholder secrets. Require endpoint scopes (`read:members`, `read:organization`, `read:statistics`, and write/import/admin scopes) plus app/audience claims. Anonymous access is an explicit documented exception only.

Rate-limit token issuance by app/member/source, audit without secrets, and prevent enumeration. Decide from audit requirements whether `LastUsedAt` must be synchronous per token or may be throttled. Store signing keys in managed secret/key storage with rotation; evaluate asymmetric signing for multiple consumers.

### 5.4 Import architecture

Stream CSV instead of `ToList()` for large files. Validate in bounded batches, prefetch only needed lookups, and remove per-row save/reload. Define transaction/partial-success policy, idempotency key/file hash, duplicate behavior, cancellation, maximum rows/bytes, and privacy-safe error artifacts. Benchmark batch sizes with SQL Server.

## 6. Dedicated benchmark-project requirement

The orphaned `Benchmarks` folder must be resolved explicitly, not merely re-included in the web project.

Create `AmsaAPI.Benchmarks/AmsaAPI.Benchmarks.csproj` as a `net10.0` console application with direct `BenchmarkDotNet` package reference and a project reference to `AmsaAPI`. Add it to the solution. Keep benchmark sources excluded from the web project's compilation so runtime dependencies and startup remain clean. Move/refactor useful existing cases into the benchmark project; delete or archive misleading cases only with migration notes.

Requirements:

- non-interactive CLI using BenchmarkDotNet switcher/filter/category support; no `Console.ReadKey`;
- Release-only documented commands and deterministic seeds;
- `MemoryDiagnoser`, environment metadata, JSON/CSV/Markdown exporters, and artifact directory keyed by SHA;
- benchmarks validate outputs so dead-code/semantic differences cannot win;
- parameters for realistic cardinalities derived from the committed seed manifest;
- separate jobs for pure mapping/serialization; database benchmarks use a controlled SQL Server fixture but are labeled integration benchmarks, not mixed with nanosecond microbenchmarks;
- update ADR 010 and benchmark README to match .NET 10/current commands and mark old numbers historical/unverified;
- no benchmark artifact is accepted without source SHA, environment, and raw result.

Required microbenchmark groups:

- flat joined-row grouping into member DTOs versus projected alternatives;
- role aggregation with repeated filtering versus pre-grouped lookup;
- DTO serialization for summary/detail/hierarchy payloads using reused serializer options/source generation candidates;
- scope/claim construction and validation helpers without cryptographic key generation noise;
- CSV row parsing/validation and lookup-key normalization;
- result/problem mapping overhead only if it can influence an architecture decision.

Endpoint-framework comparison belongs primarily in load tests. `WebApplicationFactory` plus EF InMemory may be retained as a functional smoke benchmark but must not be described as Kestrel/SQL performance.

## 7. Database, caching, and concurrency

- Capture actual SQL plans/Query Store before indexes. Candidate indexes, subject to evidence: foreign keys and joins on `Units.StateId`, `Members.UnitId`, `Levels` scope IDs, `LevelDepartments(LevelId,DepartmentId)`, `MemberLevelDepartments(MemberId,LevelDepartmentId)`; include columns only after plan review.
- Preserve existing unique member/contact and organization-name indexes and enforce assignment uniqueness if duplicate roles are invalid.
- Use optimistic concurrency for mutable registrations and member/role commands where lost updates matter.
- Bound DbContext lifetime and avoid parallel operations on one context. Cancellation must reach EF/SQL.
- Evaluate output caching only for public/reference statistics after authorization partitioning and invalidation are defined. Do not cache member details/token responses indiscriminately.
- If statistics dominate and source data changes infrequently, evaluate versioned materialized/read projections only after baseline; document freshness and rebuild.
- Move migration/seed to deployment/init tooling. App registration provisioning must be secure/idempotent and separate from web startup.

## 8. k6/NBomber/load-test plan

Use k6 for HTTP arrival-rate/concurrency tests and multipart import. NBomber may provide .NET scenario orchestration and direct consumer-contract flows with Reporting. Run against real Kestrel and SQL Server; use separate databases per run.

### Data and workloads

Seed deterministic small/medium/large and skewed datasets; record cardinalities in artifacts. Include members with zero/multiple roles, large units, all hierarchy levels, common/rare search terms, and valid/invalid/duplicate CSV files. Test both cold and warm database/buffer-cache states deliberately.

1. Token issuance with valid/invalid app secret, member, scope, and controlled rate-limit pressure.
2. Reporting consumer path: token, member-by-MKAN, state/unit lookups.
3. Paged member list/detail/by-unit/by-department/search.
4. Dashboard/unit/department/organization statistics and hierarchy.
5. Mixed read workload reflecting known consumers.
6. Member/role write concurrency and conflict behavior.
7. CSV import validation-only and commit modes at several file/batch sizes.
8. SQL transient fault/retry and dependency recovery.
9. Soak for connection pool, memory, GC, response growth, and plan stability.
10. Canonical versus alternative endpoint parity/performance during deprecation decision.

### Concurrency

Start at one virtual user, then 5 and 10, then doubled staircase stages until consumer-derived target and controlled saturation. Use constant-arrival tests for token/read bursts and isolated import concurrency so bulk work cannot hide interactive behavior. Hold stages long enough to observe pool/GC/plan effects.

### Metrics and assertions

Capture p50/p95/p99/max, RPS, error/timeout/throttle/conflict rates, response bytes, allocation rate, GC/CPU/memory/thread pool, SQL duration/roundtrips/logical reads/waits/plans, pool utilization, token issue rate, import rows/second/batch duration/error count, and retry count.

Assert contract equivalence between endpoint variants, scope enforcement, no secret bypass, stable paging/no duplicates, statistics against an oracle, idempotent writes/imports, and no partially committed import contrary to policy.

## 9. OpenTelemetry

Instrument ASP.NET Core, FastEndpoints where useful, HttpClient, EF Core/SqlClient, runtime/process, authentication, import, and custom query spans; export OTLP.

Required spans: `auth.token.issue`, `member.query`, `organization.query`, `statistics.query`, `import.parse`, `import.validate`, `import.persist`, and migration/provisioning jobs. Trace attributes use route template, endpoint implementation/canonical version, operation, result, scope set category, page-size bucket, retry count, and dataset profile—never member identity or token.

Metrics:

- request latency/count/errors by route template/status;
- canonical versus legacy route traffic;
- SQL commands/duration/rows/logical reads where available and pool state;
- response bytes and pagination sizes;
- token success/denial/throttle and issuance duration;
- import rows parsed/validated/persisted/rejected, batch duration, and queue/concurrency;
- retry/transient failures;
- allocations, GC, CPU, memory, thread pool, and exceptions.

Structured logs must replace response exposure of raw exceptions. Never log JWTs, app secrets/hashes, connection strings, member PII, raw CSV rows, or authorization headers. Define redaction, retention, and access.

## 10. Security and privacy

- Enforce app-secret validation, audience, issuer, expiry, signing key, scopes, and endpoint policies. Add least-privilege scopes for reads/writes/import/admin.
- Hash stored app secrets; reveal a generated secret once; rotate/revoke and audit use.
- Apply rate limits and request/body/page/import limits. Protect expensive search/statistics endpoints from amplification.
- Keep parameterization for raw SQL and add tests/static review for every dynamic fragment.
- Return stable Problem Details without stack traces, SQL text, or exception messages.
- Treat member contact data and roles as personal data: field minimization, encryption, retention/export/deletion, and synthetic performance fixtures.
- Validate CSV type/encoding/formula-injection risks and never trust file names/content types.
- Threat-test broken object authorization, scope confusion, app impersonation, secret brute force, token replay, mass assignment, enumeration, and denial of service.

## 11. Test requirements

- Unit tests for scope policy, secret verification, claim construction, paging validation, DTO mapping, CSV validation/idempotency, and error mapping.
- Contract/parity tests for canonical and temporary legacy endpoints, including ordering/null/status/problem semantics.
- SQL Server container integration tests for every raw SQL route, LINQ translation, migrations, indexes/constraints, collation/search semantics, optimistic conflicts, and transactions.
- Authentication integration tests for valid/invalid secret, audience, issuer, expiry, scope, inactive registration, rotation/revocation, and throttling.
- Import tests for large streaming input, cancellation, duplicate file/row, rollback/partial policy, and bounded memory.
- Fault tests for SQL transient errors, deadlocks/timeouts, process restart during import/provisioning, and client cancellation.
- Existing EF InMemory tests may remain fast logic tests but cannot validate raw SQL or serve as the release database gate.

## 12. GitHub Actions and regression strategy

Create .NET 10 workflows for restore/build, unit tests, SQL Server integration, auth/contract tests, benchmark-project compilation, formatting/analyzers, dependency/security scanning, container build, and artifacts. Protected deployment consumes the tested image.

Noise-aware performance policy:

- PRs run functional load smoke and benchmark compilation, not statistical microbenchmark gates on shared runners.
- Full BenchmarkDotNet and k6/NBomber run nightly, manually/with a `performance` label, and before release on controlled infrastructure.
- Retain BDN JSON, k6/NBomber raw output, SQL plans/Query Store extracts, environment manifest, and SHA.
- Establish variation with repeated `B0`. Fail only when a regression exceeds both practical threshold and measured noise in confirmation runs or consecutive nightlies.
- Treat one timing outlier as advisory. Security/correctness/parity failure, unbounded response/import memory, or partial commit outside policy fails immediately.
- Baseline cold/warm SQL states and endpoint implementations separately.

## 13. Deployment, containers, and Aspire

Provide a multi-stage non-root container, external configuration, health/readiness, graceful shutdown, and a separate migration/provisioning job. Production uses managed SQL connectivity, secret/key stores, HTTPS, and explicit resource limits. Readiness must not run expensive statistics.

Aspire is justified as optional local/integration orchestration for AmsaAPI, SQL Server, AMSA Reporting System, and OTLP tooling. It should make consumer contract/fault/load scenarios reproducible, not hide production architecture. Document connection pools, SQL retry budget, key rotation, autoscaling signals, and rollback. Scale based on latency/RPS/pool/CPU signals while respecting SQL capacity.

## 14. Phased backlog

### P0 — security, benchmark recovery, and baseline

- Create `AmsaAPI.Benchmarks`, move/refactor orphaned benchmarks, add it to solution, and update ADR 010.
- Add CI, OpenTelemetry, deterministic SQL seed, and k6/NBomber harness.
- Capture `B0`; mark old ADR results historical/unverified; ratify budgets.
- Enforce app-secret validation, scopes, authorization, stable errors, and rate limits.
- Add SQL Server integration coverage for raw SQL.
- Add paging/limits to unbounded reads and remove startup placeholder-secret provisioning.

### P1 — query/import consolidation

- Select canonical endpoints with parity/performance evidence and deprecate duplicates.
- Move shared query/command logic out of endpoint frameworks.
- Optimize member/statistics/hierarchy from plans and measurements.
- Stream/batch/idempotently process imports with bounded memory.
- Add concurrency control, complete indexes only where justified, and containerize migration/provisioning.

### P2 — operational and portfolio hardening

- Evaluate output cache/read projections only if telemetry justifies them.
- Add Aspire integration if it improves cross-project reproducibility.
- Run consumer, saturation, fault, soak, and release tests.
- Publish dashboards, alerts, ADRs, runbooks, absolute budgets, and case study.

## 15. Deliverables

- Dedicated `AmsaAPI.Benchmarks` project and reconciled benchmark/ADR documentation.
- Canonical endpoint inventory/decision and deprecation plan.
- k6/NBomber scripts, deterministic SQL/import seeds, and workload documentation.
- OpenTelemetry configuration, dashboards, alerts, and redaction policy.
- Expanded unit, contract, SQL, auth, import, concurrency, fault, and load tests.
- CI workflows, container/migration/provisioning assets, optional Aspire topology.
- Raw baseline/post-change evidence, SQL plans, and versioned performance budgets.
- Runbooks for SQL outage, key rotation, app-secret rotation/revocation, import recovery, migration, rollback, and regression triage.
- Portfolio case study and architecture diagrams.

## 16. Acceptance criteria

- The benchmark project builds/runs independently in Release, is in the solution, needs no interactive input, and emits SHA/environment-tagged raw artifacts. Web runtime still excludes benchmark source/dependencies.
- Historical ADR values are clearly marked unverified until reproduced; current claims link to new artifacts.
- All protected endpoints validate app secret/token/audience/scope as designed; anonymous exceptions are documented and tested.
- The ratified workloads pass p50/p95/p99/RPS/error/allocation/resource/SQL budgets on the reference environment.
- Canonical endpoints are contract-correct, paged/bounded, and selected using equivalent algorithm/data comparisons.
- Statistics/member/hierarchy results match an oracle; raw SQL is parameterized and SQL Server-tested.
- Import respects memory, transaction, idempotency, cancellation, and error-artifact policy at target sizes.
- Telemetry link
s request to auth/query/SQL/import without PII/secrets; dashboards and alerts are usable.
- CI applies controlled noise-aware regression checks; deployment supports secrets, migration isolation, readiness, graceful shutdown, and rollback.

## 17. Case-study evidence

Publish the verified original architecture and orphaned-benchmark finding; hypotheses; dataset/environment manifest; canonical-route decision; before/after p50/p95/p99/RPS/errors/allocations/CPU/memory; SQL roundtrips/logical reads/plans; response sizes; token/import behavior; and correctness/security parity. Include one rejected optimization and explain why. Separate pure microbenchmark, Kestrel/SQL staging load, and production observations.

## 18. Risks and tradeoffs

- Consolidating endpoint families reduces comparison surface but may require consumer migration.
- Raw SQL can improve control while increasing schema coupling and maintenance burden.
- One large statistics query may reduce roundtrips yet worsen plans/locking; evidence decides.
- Paging changes client behavior but is required for bounded responses.
- Secret verification and rate limiting add CPU/latency but are mandatory security costs.
- Batching imports improves throughput but increases transaction/log pressure and failure scope.
- Caching statistics/reference data adds invalidation and authorization risk.
- BenchmarkDotNet microresults do not prove SQL/Kestrel capacity; shared-runner timing is noisy; load tests do not replace production telemetry.

## Definition of Done

The rework is done only when the agreed P0/P1/P2 scope is complete; the dedicated benchmark project and load harness are reproducible; historical claims are correctly qualified; all acceptance criteria pass on the documented Kestrel/SQL Server topology; endpoint security, canonical contracts, imports, concurrency, and fault behavior are tested; telemetry and runbooks are operational; CI uses controlled noise-aware gates; deployment securely handles keys/secrets/migrations/rollback; raw before/after artifacts are retained; and the case study reports measured results without invented metrics.
