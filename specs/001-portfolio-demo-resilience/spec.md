# Spec 001 — Portfolio demo resilience and conversion surface

- Document version: 0.1
- Status: Proposed
- Date: 2026-09-08
- Owner and approval authority: Aitor Nain Mendoza Vallejo
- Governing decisions: ADR-005 (public demo boundary), ADR-006 (free-tier resilience, proposed)

## Problem

`https://nain-policy-demo-api.azurewebsites.net/demo/` is the live proof asset linked from
`naindev.com` as **"API de Operaciones de Pólizas — Probar API"**. It is the first technical
artifact a prospective B2B buyer executes.

Observed on 2026-09-08 against the live deployment:

| Observation | Measurement | Impact |
|---|---:|---|
| First `POST /api/v1/demo/run` after an idle period | `500 internal_error` after 71.3 s | The visitor's first click fails |
| Second run, backend already awake | `200 OK` after 26.7 s | No progress feedback for 27 s |
| `GET /health` cold | `200 OK` after 62.2 s | The demo page itself is slow to appear |
| Browser behaviour during the run | `Ejecutando…` with no timeout, no retry, no explanation | Reads as a broken system, not a free tier |

The backend is correct; the *free-tier operating envelope* is not represented in the contract or in
the page. Azure SQL Free uses `AutoPause`, so a resume of tens of seconds is expected behaviour,
not a fault. The current implementation converts that expected condition into an opaque `500`.

This is a defect in the approved public-demo slice, not a scope expansion: ADR-005 approves the
demo and `project-context.md` requires that portfolio claims stay evidence-based.

## Non-goals

- No change to `/api/v1/policies` contracts, domain rules or persistence schema.
- No customer identity, personal data, paid infrastructure or new product slice.
- No automatic state-changing retry of a lifecycle command without a fresh ETag (`CONC` rules stand).
- No move away from Azure App Service F1 + Azure SQL Free. The 0 EUR constraint is preserved.

## Requirements

### Backend resilience

- `REQ-001` When the SQL Server connection is resuming or transiently unavailable, the API shall
  translate the failure into `503 Service Unavailable` with stable code `service_unavailable` and a
  `Retry-After` header, instead of `500 internal_error`.
- `REQ-002` When the public demo prune step fails for any reason, the system shall log the failure
  and continue the run, because pruning is opportunistic housekeeping and not part of the
  demonstrated lifecycle evidence.
- `REQ-003` When a SQL command is issued, the configured command timeout shall be long enough to
  absorb an Azure SQL serverless resume, and shall be configurable per deployment.
- `REQ-004` When the API starts, the SQL resilience settings (retry count, retry delay, command
  timeout) shall come from validated configuration with safe defaults.
- `REQ-005` When `/health/ready` is called while SQL Server is resuming, it shall return `503`
  without throwing, so the page can use it as a warm-up probe.

### Demo page conversion surface

- `REQ-010` When the demo page loads, it shall probe `/health/ready` and show an explicit
  "waking the free-tier database" state until the backend reports ready.
- `REQ-011` When a run is in progress, the page shall show elapsed time and a staged explanation,
  and shall never present an unbounded silent wait.
- `REQ-012` When a run returns `503` or times out, the page shall retry automatically a bounded
  number of times and explain the free-tier resume in the visitor's language.
- `REQ-013` When a run succeeds, the page shall display, per step, the HTTP method and path, the
  status code, the resulting resource status, the ETag and the measured server-side duration.
- `REQ-014` The page shall offer a copy-ready `curl` command so a technical visitor can reproduce
  the run independently.
- `REQ-015` The page shall declare Open Graph and Twitter Card metadata consistent with
  `naindev.com`, so shared links render as a branded card.
- `REQ-016` The page shall be operable by keyboard, expose run state to assistive technology, and
  respect `prefers-reduced-motion` and `prefers-color-scheme`.

### Verifiable evidence

- `REQ-020` Continuous integration shall execute the SQL Server integration suite against a real
  SQL Server instance, so the README claim "tests against real SQL Server" is machine-verified.
- `REQ-021` Continuous integration shall publish a code coverage summary for every run.
- `REQ-022` The repository shall pin editor and analyzer conventions in `.editorconfig` so that
  `dotnet format --verify-no-changes` is deterministic across machines.
- `REQ-023` Application-layer rules shall be covered by a fast unit-test project that requires no
  database.
- `REQ-024` Dependency and code scanning shall run on a schedule without manual action.

### Repository constitution

- `REQ-030` The repository shall carry an `AGENTS.md` canonical constitution and a `CLAUDE.md` that
  imports it, matching the owner's cross-project standard.
- `REQ-031` Every approved business rule shall be traceable to the test that demonstrates it.
- `REQ-032` The repository shall record a dated changelog of relevant behavioural changes.

## Acceptance evidence

A change is done when all of the following hold:

1. Release build passes with 0 warnings and 0 errors.
2. `dotnet format --verify-no-changes` passes.
3. Domain, application and SQL Server suites pass locally and in CI.
4. A cold public run returns either `200` or a documented `503` with `Retry-After`, never `500`.
5. The demo page shows staged progress, per-step latency and a reproducible `curl` command.
6. `docs/traceability.md` maps every referenced requirement to a named test.

## Open questions

- `OPEN-012` Should the demo page be mirrored as a static page on `naindev.com` so the cold start
  happens *after* the visitor sees content? That requires a CORS decision on this API and a change
  in the portfolio repository. Blocked pending owner decision.
- `OPEN-013` Should the repository be renamed from `API-Gestion-Financiera` to a name matching the
  product (`policy-operations-api`)? It would break the link recorded in the portfolio's
  `projects.json`. Owner decision.
