# ADR-006: Free-tier resilience and public-demo evidence

- Identifier: ADR-006
- Date: 2026-09-08
- Status: Approved
- Decision owner: Aitor Nain Mendoza Vallejo

## Context

ADR-005 approved a public demo on Azure App Service F1 and the Azure SQL free offer, and
explicitly accepted cold starts and quota stops as consequences of the zero-cash boundary. What it
did not settle is **how the API and the page should behave while that free tier is waking up**.

Measured against the live deployment on 2026-09-08:

| Request | Result | Elapsed |
|---|---|---:|
| `POST /api/v1/demo/run` after an idle period | `500 internal_error` | 71.3 s |
| `POST /api/v1/demo/run` immediately afterwards | `200 OK` | 26.7 s |
| `GET /health` cold | `200 OK` | 62.2 s |
| `GET /health/ready` warm | `200 OK` | 0.5 s |

The failing run is the one a first-time visitor from `naindev.com` gets. Three separate causes
combine:

1. Azure SQL Serverless resumes from `AutoPause` on the first connection. The provider default
   command timeout of 30 seconds truncates that resume, so an expected condition surfaces as a
   failure.
2. The demo's opportunistic cleanup is the first database work in the request. When it fails
   during the resume, it aborts the whole run even though it demonstrates nothing.
3. Every unmapped exception becomes `500 internal_error`, which tells the caller "this system is
   broken" when the truth is "this system is waking up".

The page compounds it: it issues one `fetch` with no timeout, no retry and no explanation, so the
visitor watches `Ejecutando…` for over a minute before seeing a generic error.

## Decision

### Contract

- Add `503 Service Unavailable` with stable code `service_unavailable` and a `Retry-After` header
  to the demo and policy endpoints, returned when the database reports a recognised transient
  condition. `500 internal_error` is reserved for genuine defects.
- Recognition is a closed set of SQL Server and Azure SQL error numbers, plus timeouts, held in
  `SqlTransientErrors` in the Infrastructure layer. Anything outside that set stays a `500`.
- Public-demo cleanup becomes best effort. A failed cleanup is logged and the run continues; the
  run response reports `cleanupCompleted` so the degradation is visible rather than hidden.
- `PublicDemoStepResponse` gains `method`, `path` and `durationMs`; the run response gains
  `totalDurationMs` and `cleanupCompleted`. These are additive fields.

### Configuration

- SQL resilience moves into a validated `PolicyOperations:SqlResilience` section with
  `MaxRetryCount`, `MaxRetryDelaySeconds`, `CommandTimeoutSeconds` and `RetryAfterSeconds`.
- The default command timeout becomes 60 seconds so a serverless resume is absorbed rather than
  truncated.

### Page

- Probe `/health/ready` before offering the run, and state plainly that the free-tier database is
  waking up.
- Bound every request with a timeout, retry a limited number of times on `503`, and show elapsed
  time throughout.
- Show, per step, the HTTP method, path, status, resulting status, ETag and measured duration, and
  offer a `curl` command so the run can be reproduced without trusting the page.

### Evidence

- CI runs the SQL Server suite against a real SQL Server service container, so "verified against
  real SQL Server" is proved by the pipeline rather than asserted in the README.
- Application-layer rules gain a database-free unit-test project.

## Consequences and trade-offs

- A visitor's first click either succeeds or receives an honest, actionable `503`; it no longer
  reports an internal error for a documented operating condition.
- A longer command timeout holds an F1 request thread for longer. The demo endpoint remains rate
  limited, and the timeout is configurable per deployment.
- Best-effort cleanup means expired synthetic rows can survive longer than the retention window
  when the database is under stress. `DEMO-007` still constrains *what* may be deleted; only the
  guarantee that cleanup runs is relaxed.
- Mapping transient errors to `503` risks masking a real defect if the recognised set is too wide.
  The set is explicit, small and reviewable, and every `503` is logged with its exception.
- The zero-cash boundary is unchanged. Nothing here requires a paid tier, and none of it removes
  the cold start; it makes the cold start legible instead of pretending it does not exist.

## Rejected alternatives

- **Enable Always On / a paid App Service tier:** rejected. It would remove the cold start but
  breaks the 0 EUR constraint approved in `project-context.md`.
- **Keep-alive pinger to prevent auto-pause:** rejected. It would consume the free vCore allowance
  continuously and reach the monthly limit faster than real visitors do.
- **Retry the lifecycle commands server-side after an uncertain result:** rejected. It contradicts
  `CONC` rules: no state-changing retry without a freshly read ETag.
- **Hide the wait behind an animation:** rejected. Overstating availability is exactly what
  `project-context.md` forbids; the free-tier boundary is part of the engineering story.
- **Move the demo page to `naindev.com` as a static page:** deferred, not rejected. It would put
  content in front of the visitor before the backend wakes, but it needs a CORS decision on this
  API and a change in the portfolio repository. Tracked as `OPEN-012`.

## Affected components and documents

- `PolicyOperations.Api`: exception handler, resilience options, demo controller and pruner,
  security headers, static-file caching, `/demo/` page.
- `PolicyOperations.Infrastructure`: `SqlTransientErrors`.
- New `PolicyOperations.Application.Tests`.
- `.github/workflows/ci.yml`, `.github/workflows/codeql.yml`, `.github/dependabot.yml`.
- `docs/business-rules.md`, `docs/architecture.md`, `docs/public-demo.md`,
  `docs/project-context.md`, `docs/traceability.md`, `docs/changelog.md`, `README.md`.

## Migration and recovery impact

None. No schema change, no migration and no data movement. Reverting means restoring the previous
exception mapping and page; the database is untouched either way.

## Implementation evidence

Implemented locally on 2026-09-08:

- Release build: 0 warnings, 0 errors.
- `dotnet format --verify-no-changes`: passes.
- 22 domain, 21 application and 25 SQL Server tests pass against SQL Server 2025
  (`17.0.4065.4`); 21 of the SQL tests exercise a real database.
- The public demo response was observed to carry per-step `method`, `path` and `durationMs`,
  `totalDurationMs` and `cleanupCompleted`.

**Not yet verified:** the deployed environment. Until this change is released to Azure, the
`500` observed on 2026-09-08 remains reproducible in production.

## Reconsideration triggers

Reconsider when a qualified buyer requires an availability guarantee, when the free offer changes
its resume behaviour, or when `OPEN-012` is decided and the page stops being served by the API.

## Approval record

Not approved. Awaiting the decision owner. Until approval, the rules this ADR introduces are
recorded as `Proposed` in `docs/business-rules.md` even though the code implements them.
