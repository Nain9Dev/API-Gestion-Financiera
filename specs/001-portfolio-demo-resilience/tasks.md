# Tasks 001 — Portfolio demo resilience and conversion surface

- Document version: 0.1
- Date: 2026-09-08
- Labels: `[A]` agent completes, `[M]` mixed, `[H]` owner only

## Status

| ID | Task | Label | State |
|---|---|---|---|
| `T-001` | Centralise transient SQL error numbers in Infrastructure | `[A]` | Done |
| `T-002` | Map transient SQL failure to `503 service_unavailable` with `Retry-After` | `[A]` | Done |
| `T-003` | Make public-demo pruning non-fatal and logged | `[A]` | Done |
| `T-004` | Add validated `SqlResilienceOptions` with explicit command timeout | `[A]` | Done |
| `T-005` | Measure and expose per-step server-side duration | `[A]` | Done |
| `T-006` | Rebuild the demo page: warm-up probe, staged progress, bounded retry, curl, a11y, OG | `[A]` | Done |
| `T-007` | Add `.editorconfig` matching the existing style | `[A]` | Done |
| `T-008` | Add `PolicyOperations.Application.Tests` (no database) | `[A]` | Done |
| `T-009` | Add SQL Server service container job to CI | `[A]` | Done |
| `T-010` | Add coverage collection and summary to CI | `[A]` | Done |
| `T-011` | Add CodeQL workflow and Dependabot configuration | `[A]` | Done |
| `T-012` | Add `AGENTS.md` and `CLAUDE.md` | `[A]` | Done |
| `T-013` | Add `docs/traceability.md` and `docs/changelog.md` | `[A]` | Done |
| `T-014` | Add `SECURITY.md` | `[A]` | Done |
| `T-015` | Record ADR-006 as `Proposed` | `[A]` | Done |
| `T-016` | Update `README.md`, `docs/README.md`, `docs/architecture.md`, `docs/business-rules.md`, `docs/public-demo.md`, `docs/project-context.md` | `[A]` | Done |
| `T-017` | Apply global security headers and static-asset caching | `[A]` | Done |
| `T-018` | Approve or reject ADR-006 and flip the affected rules to `Approved` | `[H]` | Done (Approved) |
| `T-019` | Deploy the fix to Azure and re-run the cold public smoke test | `[M]` | Pending owner deployment |
| `T-020` | Decide `OPEN-012` (static demo page on naindev.com + CORS) | `[H]` | Done (Approved: CORS enabled for localhost:5375 & naindev.com with ETag and Retry-After) |
| `T-021` | Decide `OPEN-013` (commercial brand naming) | `[H]` | Done (Approved: Brand name selected as NainPolicy for naindev.com portfolio) |

## Session handoff

- **Done and verified locally:** `T-001` to `T-018`, `T-020`, `T-021`. Upgraded to .NET 10.0.13,
  migrated OpenAPI to Scalar.AspNetCore, built PolicyOperations.Client (Vue 3 + TypeScript) with
  interactive lifecycle workbench, concurrency lab (ETag / rowversion HTTP 412), immutable audit
  trail and resilience telemetry probe. Release build 0 warnings / 0 errors, `dotnet format` clean,
  43 unit tests passed, Vue client build clean.
- **Not verified:** the Azure production deployment has not run, so production smoke test
  awaits owner deployment (`T-019`).
- **Blocked:** `T-019` needs owner deployment.
