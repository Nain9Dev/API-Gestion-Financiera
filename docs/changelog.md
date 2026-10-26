# Changelog

- Document version: 0.1
- Status: Active
- Date: 2026-09-08

Relevant behavioural changes, newest first. Dates are the date the change was made, not released.
A change is listed only when it alters behaviour, contracts, evidence or operating boundaries.

## 0.11 — 2026-10-26 — Azure redeploy

Deployed the current build to the existing App Service (`T-019`, `T-116`): ADR-006 resilience and
the `policy.naindev.com` CORS origin are now live. No schema or setting change. Evidence in
`docs/public-demo.md`.

## 0.10 — 2026-10-22 — Live architecture blueprint and honest execution source

Governing decision: ADR-007 (`Proposed`), spec 002.

### Added

- `PolicyOperations.Client` opens on a live architecture blueprint: an SVG map of Client, API,
  Application, Domain (three lifecycle states), Infrastructure and SQL Server (`Policies` and the
  append-only `PolicyTransitions` ledger). Running the scenario replays the returned steps through
  the map; a `412` is drawn as a rejected path at the concurrency guard and each state change
  appends a ledger row.
- Every run is labelled `Live` (response from the API) or `Simulated` (in-browser fallback).
- `VITE_API_BASE_URL` with a committed `.env.production` pointing the Pages build at the Azure API.
- `https://policy.naindev.com` added to the API CORS origins (`PreflightFromPolicySubdomainIsAllowed`).
- Vitest in the client: `blueprintTimeline.test.ts` and `api.test.ts` (16 tests).
- Telemetry for the ecosystem architecture drawer is emitted for live and simulated runs.

### Changed

- Client theme moved to the NainDev brand tokens (Urbanist, IBM Plex Mono, brand blue, green and
  red, 4 px radius, blueprint grid). Secondary views keep their function with a title and one
  subtitle; explanatory paragraphs removed.
- Client wire contracts aligned to the API records (`operation`, `status`, `result`,
  `resourceStatus`, `dataRetentionHours`, `policy`, `occurredAtUtc`, `actorSubject`,
  `correlationId`). Before this change a live response rendered undefined values.

## 0.9 — 2026-10-22 — OpenAPI contract evidence restored after the Scalar migration

Specification: `specs/003-openapi-contract-test/`.

### Fixed

- The OpenAPI document moved to `/openapi/v1.json` in 0.8 and fell under the authenticated-user
  fallback policy, so anonymous requests received `401` and the `/docs` reference could not load
  its own document. The document, `/docs` and the `/swagger` redirect are anonymous again; every
  other endpoint keeps the fallback policy.
- The Swashbuckle operation filter removed in 0.8 was not replaced, so the native document
  described `If-Match` as optional without an example, listed `text/plain` and `text/json` on
  every response and published no security scheme. `BearerSecurityDocumentTransformer` and
  `PolicyOperationTransformer` restore the Bearer scheme with its global requirement, the empty
  requirement on anonymous operations, the required quoted `If-Match` example and the
  `application/json` / `application/problem+json` media types.
- `SwaggerExplainsQuotedEtagAndDocumentsJsonResponses` still requested the removed Swagger path.
  Renamed to `OpenApiDocumentExplainsQuotedEtagAndDocumentsJsonResponses`, pointed at the native
  document and extended to assert the Bearer scheme and the global requirement.

### Verified

- Local SQL Server 2025, Release build, 0 warnings, `dotnet format` clean: 22 domain, 25
  application and 25 integration tests passed, 0 skipped.

## 0.8 — 2026-10-14 — NainPolicy Client, OpenAPI Scalar migration and commercial alignment

Governing decision: ADR-006 (`Approved`), `OPEN-012` and `OPEN-013`.

### Added

- `PolicyOperations.Client` on port `5375`: Vue 3 + TypeScript + Vite interactive engineering showcase with dark glassmorphic UI matching the `naindev.com` portfolio standard:
  - Interactive Finite State Machine (FSM) workbench covering `Draft -> Active -> Cancelled`.
  - Optimistic Concurrency Laboratory showcasing two competing operators with `ETag` / `rowversion`, deterministically provoking HTTP `412 Precondition Failed` with RFC 7807 problem details.
  - Append-only immutable audit trail transitions timeline.
  - Real-time cloud resilience probe measuring latency and cold-start wake-up metrics against `/health` and `/health/ready`.
- Direct CORS policy supporting `http://localhost:5375` and `https://naindev.com` with exposed `ETag` and `Retry-After` headers.
- Native `Microsoft.AspNetCore.OpenApi` (.NET 10) and `Scalar.AspNetCore` modern API explorer mounted at `/docs`.
- Central Package Management direct pin for `System.Security.Cryptography.Pkcs 10.0.13` fixing transitive vulnerability warnings `NU1902` and `NU1903`.

### Changed

- Replaced deprecated `Swashbuckle.AspNetCore` with `Scalar.AspNetCore` and redirected `/swagger` to `/docs`.
- Formalized commercial product brand as **NainPolicy** across portfolio documentation.

## 0.7 — 2026-09-08 — Free-tier resilience and demo evidence

Governing decision: ADR-006 (`Approved`). Specification: `specs/001-portfolio-demo-resilience/`.

### Fixed

- A public demo run issued after an idle period returned `500 internal_error`. The Azure SQL
  Serverless resume exceeded the provider's default 30-second command timeout, and the failure was
  mapped to a generic internal error. Measured on the live deployment: `500` after 71.3 s, then
  `200` after 26.7 s once the database was awake.
- Public-demo cleanup aborted the whole run when it failed. Cleanup is opportunistic housekeeping
  and now degrades: the failure is logged, the run continues, and the response reports
  `cleanupCompleted`.

### Added

- `503 service_unavailable` with `Retry-After` for recognised transient database conditions.
  `500 internal_error` is now reserved for genuine defects.
- `SqlTransientErrors` in the Infrastructure layer: an explicit, reviewable set of SQL Server and
  Azure SQL transient error numbers.
- `PolicyOperations:SqlResilience` configuration section with validated retry count, retry delay,
  command timeout (default 60 s) and `Retry-After` value.
- `method`, `path` and `durationMs` on each public-demo step; `totalDurationMs` and
  `cleanupCompleted` on the run response.
- Baseline security headers on every response, HSTS outside development, and short-lived caching
  for demo static assets.
- `PolicyOperations.Application.Tests`: 21 database-free tests covering the currency allow-list,
  pagination limits, ETag shape, the stale-version pre-check and audit orchestration.
- Four integration tests: per-step evidence, baseline security headers, the demo page content
  security policy and transient-condition recognition.
- CI now runs the SQL Server suite against a real SQL Server service container, collects coverage
  and fails on vulnerable NuGet packages. CodeQL analysis and Dependabot updates were added.
- `.editorconfig`, `AGENTS.md`, `CLAUDE.md`, `SECURITY.md`, `docs/traceability.md` and this file.

### Changed

- The `/demo/` page was rebuilt: readiness pre-flight, staged progress with elapsed time, bounded
  automatic retry on `503`, per-step route/status/ETag/latency, a copy-ready `curl` command,
  Open Graph metadata, keyboard and screen-reader support, and the `naindev.com` palette.

### Not verified

- The deployed environment. Until this change is released to Azure, the `500` observed on
  2026-09-08 remains reproducible in production.

## 0.6 — 2026-08-02 — Live zero-cost public demo

- Deployed the public demo to Azure App Service F1 and Azure SQL Free in France Central.
- Recorded the quota alert boundary and the `AutoPause` cutoff.
- Enabled transient SQL retries for serverless wake-up and moved demo cleanup into one retryable
  transaction.
- Public HTTPS smoke: liveness, readiness and the five-step run all returned `200`.

## 0.5 — 2026-08-02 — One-click public demo

- Added the anonymous fixed-scenario endpoint, the same-origin demo page, the fixed-window rate
  limit, the dedicated demo organization and 24-hour retention.
- Separated process liveness from SQL readiness.

## 0.4 — 2026-08-02 — OpenAPI accuracy

- Documented the quoted `If-Match` example and aligned OpenAPI response media types.

## 0.3 — 2026-08-02 — Lifecycle, security and audit

- Implemented the policy lifecycle, organization-scoped authorization, ETag concurrency, the audit
  trail, the MIT license and the local demo walkthrough.

## 0.2 — 2026-08-01 — First vertical slice

- Approved the product and architecture decisions and implemented create, get and list on .NET 10
  with real SQL Server verification.

## 0.1 — 2026-08-01 — Repository assessment

- Reviewed the pre-1.0 prototype and recorded its contradictions and material findings.
