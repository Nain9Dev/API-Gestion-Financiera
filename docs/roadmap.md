# Roadmap and gates

- Document version: 0.6
- Status: Active
- Date: 2026-08-02

## Where the project stands

The local and public technical demos are complete for the approved scope. The public page runs on the exact F1 and Azure SQL Free boundary and has passed its HTTPS smoke test. The next priority is commercial evidence, not another product slice.

## Gate overview

| Gate | State | Verified evidence | Remaining condition |
|---|---|---|---|
| G0 Product foundation | Passed | ADR-001 through ADR-004 approved | Risk remains separate |
| G1 Implementation ready | Passed | Contracts, security boundary and migration strategy defined | None for current scope |
| G2 First vertical slice | Passed | Create/get/list and SQL verification | Preserve regression gate |
| G3 Local technical demo | Passed | Full local suite and observed JWT lifecycle smoke on version 0.5 | Local/synthetic boundary |
| G3.1 Portfolio publication evidence | Passed | Public HTTPS page, Azure SQL run, readiness and live link | Preserve zero-cost controls and truthful claims |
| G4 Commercial validation | Ready, not started | 40-hour and 0 EUR guardrails | Run bounded validation separately |
| G5 Customer pilot | Not authorized | None | Commercial, identity, privacy and operation gates |

## Completed lifecycle slice

`Create Draft -> activate complete policy -> reject stale update -> cancel -> inspect audit`

Acceptance evidence:

- exact transitions enforced in Domain;
- amount has ISO-shaped configured currency;
- activation requires an opaque insured-party reference and valid coverage dates;
- cancellation requires a reason;
- JWT roles and trusted organization claim enforced;
- policy number, queries and transitions isolated by organization;
- strong ETag and `If-Match` exposed in Swagger;
- stale command returns `concurrency_conflict` without an audit insert;
- policy update and audit insert persist together;
- migrations refuse invented organization/currency and destructive down behavior;
- local JWT smoke completed `Draft -> Active -> Cancelled` with two audit rows;
- Release build passes with warnings as errors;
- 22 domain and 18 SQL Server integration/migration tests pass.

## Completed public-demo slice

`One portfolio click -> fixed synthetic API run -> visible five-step evidence`

Acceptance evidence:

- no registration, download, token or request body;
- normal policy endpoints remain JWT protected;
- one dedicated demo organization and server-side actor;
- 24-hour opportunistic cleanup restricted to that organization;
- configurable fixed-window rate limit and stable `429` response;
- separate process liveness and SQL readiness;
- same-origin page with restrictive browser policy and no external assets;
- local browser run observed `201 -> 200 -> 412 -> 200 -> 200` and two audit transitions;
- Release build passes with 0 warnings and 0 errors;
- 22 domain and 21 SQL Server integration/migration tests pass.

## Checks run for the public release

- Release build: 0 warnings and 0 errors.
- Release build and formatter pass on version 0.6.
- All 43 tests pass on version 0.6; 21 exercise real SQL Server 2025.
- The changed serverless retry and cleanup path passed the deployed Azure five-step scenario and returned two audit transitions.
- EF Core model has no pending migration changes.
- NuGet direct/transitive vulnerability audit reports no known findings from configured sources.
- Formatter verification passes.
- Swagger JSON contains the Bearer scheme, one global security requirement, a required quoted `If-Match` example and accurate JSON response media types.
- Test cleanup leaves zero temporary SQL databases.
- `PolicyOperationsLocalDemo` has all three migrations, one synthetic smoke policy and two audit transitions; it contains no non-synthetic policy number.

## Recommended next work within the 40-hour guardrail

### 1. Look for a real commercial signal before writing more product code

- contact 10 qualified insurance-operations or technical profiles;
- validate the workflow problem, current impact and buyer responsibility;
- request a dated second conversation rather than generic feedback;
- discuss a bounded pilot only after a real problem signal;
- apply the Continue/Change/Pause/Stop thresholds in the economic brief.

### 2. Maintain the portfolio demo without expanding scope

- check the live link periodically and after each deployment;
- confirm the plan remains F1 and SQL remains `AutoPause` before changing Azure settings;
- investigate any cost alert immediately and disable the demo if the free boundary changes;
- keep migration-on-startup disabled except during a reviewed migration release;
- record active owner time against the 40-hour cap.

### 3. Customer-pilot foundation — only after evidence

- select an external OpenID Connect issuer;
- define user membership and organization administration;
- approve data classification, retention, export and deletion;
- select licensed hosting/database and prove backup restoration;
- add actionable logging, SQL readiness, metrics and incident runbook;
- define support, onboarding and recovery responsibility.

## Explicitly deferred

- risk scoring or underwriting advice;
- customer-specific forks;
- billing, payments and unlimited integrations;
- microservices, queues, Kubernetes or distributed transactions;
- real personal data;
- production on SQL Server Standard Developer or the owner's computer;
- load optimization before a capacity test identifies a bottleneck.

## Stop conditions

- 40 additional owner-hours are exhausted without stronger commercial evidence;
- portfolio claims exceed verified behavior;
- a public endpoint is exposed using local JWTs;
- a paid Azure resource or automatic usage continuation is enabled;
- real personal data is introduced before the pilot gate;
- a migration assigns organization or currency without verified row ownership;
- optional features displace validation, security or recovery work.

## Remaining owner decisions

No decision blocks use of the local technical demo. New approval is required before:

1. implementing risk assessment;
2. selecting a public/customer identity provider;
3. expanding the public backend beyond the fixed synthetic demo;
4. processing personal data;
5. spending money or exceeding the 40-hour guardrail.
