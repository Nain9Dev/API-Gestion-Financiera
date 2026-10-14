# Plan 001 — Portfolio demo resilience and conversion surface

- Document version: 0.1
- Status: Proposed
- Date: 2026-09-08
- Implements: `spec.md` in this directory

## Design decisions

### 1. Transient SQL failure becomes an honest `503`

`ApiExceptionHandler` gains one mapping ahead of the catch-all: a `SqlException` whose number is in
the Azure SQL resume/transient set, or a `TimeoutException` raised while the connection is being
established, maps to `503 service_unavailable` with `Retry-After: 20`.

Rationale: an exhausted free-tier resume window is a *known operating condition of this
deployment*, not an unexpected internal error. `500 internal_error` is reserved for defects.
Returning `503` also lets any client — the demo page, a monitor, a visiting engineer — behave
correctly without parsing prose.

The transient number set is centralised in `SqlTransientErrors` in the Infrastructure layer so the
handler does not embed provider knowledge in the API layer.

### 2. Pruning degrades instead of failing the run

`PublicDemoDataPruner.PruneAsync` returns a result instead of throwing. `PublicDemoController`
logs a warning and proceeds. Evidence for the visitor is the lifecycle, not the housekeeping.

`DEMO-007` is preserved: when pruning does run, it still deletes only expired rows inside the
configured demo organization, inside one transaction, inside the execution strategy.

### 3. Resilience settings become configuration

A new `SqlResilienceOptions` section (`PolicyOperations:SqlResilience`) supplies
`MaxRetryCount`, `MaxRetryDelaySeconds` and `CommandTimeoutSeconds`, validated on start.
Defaults preserve today's behaviour except the command timeout, which becomes explicit (`60 s`)
so that a serverless resume is absorbed rather than truncated at the provider default of 30 s.

### 4. The page models the free tier instead of hiding it

The page performs a readiness pre-flight against `/health/ready`, renders a warm-up state, then
runs the scenario with an `AbortSignal` timeout and bounded automatic retries on `503`.
Per-step server-side durations are added to `PublicDemoStepResponse`, so the displayed latency is
measured by the API, not guessed by the browser.

This is presentation plus one additive response field. No lifecycle contract changes.

### 5. CI proves the claim instead of restating it

The workflow gains a `sqlserver` job using the official `mssql/server` service container. It sets
`POLICY_OPERATIONS_TEST_SQLSERVER` so `SqlServerFactAttribute` stops skipping, and the suite that
`README.md` advertises actually runs on every push. Coverage is collected and summarised.

A separate scheduled workflow runs CodeQL. Dependabot keeps NuGet and Actions current.

## Layering

No layer boundary moves. Additions land where they belong:

| Concern | Layer | Reason |
|---|---|---|
| Transient SQL error numbers | Infrastructure | Provider knowledge |
| Prune outcome type | API (`Demo`) | Demo façade concern only |
| Step duration measurement | API (`Demo`) | Presentation evidence, not a domain rule |
| Resilience options | API composition root | Deployment configuration |
| Application rule tests | New test project | No database dependency |

`PolicyOperations.Domain` and `PolicyOperations.Application` are untouched by this plan except for
the new test project that exercises them.

## Risks

| Risk | Mitigation |
|---|---|
| A real defect gets hidden behind `503` | Only a closed set of transient/connection errors maps to `503`; everything else stays `500` and is logged |
| Longer command timeout holds F1 threads | Timeout is configurable and the demo endpoint stays rate limited |
| CI SQL container drifts from SQL Server 2025 | The container image tag is pinned and recorded in `docs/architecture.md` |
| Contract addition without approval | Recorded as ADR-006 `Proposed`; business rules carry state `Proposed` until the owner approves |
