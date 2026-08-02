# Project context

- Document version: 0.6
- Status: Active
- Date: 2026-08-02
- Scope: Product, authority, readiness, boundaries and blockers
- Owner and approval authority: Aitor Nain Mendoza Vallejo

## 1. Identity and authority

| Field | Value | State |
|---|---|---|
| Repository | `Nain9Dev/API-Gestion-Financiera` | Verified scope |
| Product name | Insurance Policy Operations API | Approved |
| Environment | Personal portfolio project | Confirmed |
| Product direction | B2B policy operations technical demo | Approved in ADR-001 |
| Architecture | .NET 10 modular monolith | Approved in ADR-002 and implemented |
| Lifecycle/security | Organization-scoped JWT, ETag and audit | Approved in ADR-003 and implemented |
| Repository license | MIT | Approved in ADR-004 and added |
| Public demo boundary and hosting target | One-click synthetic façade; Azure F1 + Azure SQL Free | Approved in ADR-005 |
| Current milestone | Public portfolio demo | Live and verified over HTTPS |
| Incremental cash budget | 0 EUR before commercial evidence | Approved constraint |
| Owner-hours cap | 40 additional hours from 2026-08-02 | Approved guardrail |
| Data boundary | Synthetic data only | Approved |

Code and migrations are authoritative for implemented behavior. Approved rules and ADRs govern changes. Draft risk and commercial hypotheses are not implementation authority.

## 2. Product definition

Portfolio API demonstrating reliable policy lifecycle operations with trusted organization scope, explicit domain rules, auditability, stale-write protection and real SQL Server persistence.

The customer hypothesis remains a small insurance brokerage or insurtech operations team. Buyer urgency, workflow fit and willingness to pay remain unvalidated. Technical readiness is portfolio evidence, not commercial validation.

## 3. Readiness

| Gate | State | Evidence | Next condition |
|---|---|---|---|
| Product foundation for implemented policy slice | Passed | Product, lifecycle, security and license decisions approved | Preserve traceability |
| Implementation ready | Passed | API, persistence, migration, authorization and test contracts defined | None for implemented scope |
| First create/get/list slice | Passed | .NET 10 and real SQL tests | Preserve regression gate |
| Local technical demo | Passed | JWT smoke, lifecycle, concurrency and audit verified with synthetic data | Preserve regression gate |
| One-click public-demo implementation | Passed | Live five-step run against Azure SQL Free | Preserve the zero-cost and synthetic-data boundary |
| Portfolio publication evidence | Passed | Public HTTPS page, readiness and observed result | Keep the link monitored and claims evidence-based |
| Commercial validation | Ready but not started | 40-hour guardrail approved; no market evidence | Conduct bounded interviews separately |
| Customer pilot | Not authorized | No external issuer, privacy, operations, hosting or contract evidence | Pass commercial and operational gates |

## 4. Implemented scope

- create organization-scoped Draft policies with amount and configured currency;
- read one policy, list bounded pages and return cross-organization resources as not found;
- activate Draft with opaque insured-party reference and coverage dates;
- cancel Draft or Active with a reason; Cancelled is terminal;
- authenticate JWTs and authorize reader/operator roles;
- derive actor and organization from validated claims;
- expose and require ETags for lifecycle mutations;
- map SQL Server `rowversion` conflicts to safe `412` responses;
- append transition audit in the same transaction as the policy update;
- expose ordered transition history;
- preserve old migration history and stop unsafe ownership/currency inference;
- provide Swagger and `.http` local demo clients;
- provide a one-click synthetic demo page and anonymous fixed scenario, disabled by default;
- limit public-demo runs, prune only expired demo-organization data and expose SQL readiness separately;
- retry transient SQL Serverless failures while keeping cleanup deletes in one retryable transaction;
- run the public demo on App Service F1 and Azure SQL Free with HTTPS and an exhaustion cutoff;
- verify behavior against SQL Server 2025 Standard Developer.

The pre-1.0 `/api/policies` prototype was intentionally replaced by `/api/v1/policies` before known external consumers existed.

## 5. Deliberately not implemented

- issuing production tokens, passwords, MFA or account recovery;
- organization/user administration and invitations;
- a selected customer OpenID Connect provider;
- policy editing, transfer between organizations or reactivation;
- personal identity/contact/document storage;
- audit retention, export, legal hold or administrator search;
- generic idempotency keys or automatic state-changing retries;
- risk assessment, billing, integrations, documents, messaging or distributed services;
- customer hosting, backup, restore, alerting and support procedures. The free demo has only a quota warning, not customer operations.

These are not defects in the local technical demo. They remain separate decisions or later gates.

## 6. Data and security boundaries

- Requests are untrusted and server validation is authoritative.
- JWT actor and organization claims are the only scope source.
- Connection strings and signing keys remain outside source control.
- Local tokens use `dotnet user-jwts` and must not be accepted from the internet.
- The public demo accepts no request body and uses a fixed synthetic organization and actor.
- The `/health` endpoint is process liveness; `/health/ready` checks SQL connectivity.
- Integration tests delete only databases matching protected prefixes.
- `PolicyOperationsLocalDemo` contains synthetic demo evidence only.
- A customer pilot requires classification, least privilege, retention, export/deletion, backups, restore tests and incident handling.

## 7. Technology and cost

| Responsibility | Baseline | Cost boundary |
|---|---|---|
| Runtime | .NET 10 LTS | Free runtime and supported patches |
| Authentication validation | Microsoft JWT Bearer | MIT; local issuer only |
| Persistence | EF Core 10 and SQL Server | Provider open source; SQL license depends on edition/use |
| Local database | SQL Server 2025 Standard Developer 17.0.4065.4 | Free for development/test, not production |
| API description | Swashbuckle 10.2.3 | MIT |
| Tests | xUnit and Microsoft test platform | Apache-2.0/MIT packages |
| Repository | MIT License | Commercial reuse permitted under license terms |
| Public demo compute | Azure App Service F1 in France Central | Provisioned as Linux F1 Free; no SLA and cold starts allowed |
| Public demo database | Azure SQL Database free offer in France Central | `useFreeLimit=true`, `AutoPause`, 32 GB, LRS, no paid overage |
| Demo warning | Azure Monitor metric alert | Email at 1,000 vCore-seconds remaining; one active metric rule |

No paid service or automatic conversion to metered usage is authorized. The deployed SQL database cannot continue as paid overage. The preventive alert does not stop the database; `AutoPause` is the authoritative cutoff when the free monthly allowance is exhausted.

## 8. Current verification

- Release build for version 0.6: 0 warnings, 0 errors.
- Domain tests for version 0.6: 22 passed.
- SQL Server API/security/OpenAPI tests for version 0.6: 17 passed.
- SQL Server migration tests for version 0.6: 4 passed.
- Total for version 0.6: 43 passed; 21 use real SQL Server 2025.
- The changed retry and cleanup path was exercised by the live Azure run: five successful expected steps, final `Cancelled` policy and two transitions.
- Legacy active/cancelled rows and timestamps remain preserved by the lifecycle migration.
- Duplicate legacy normalization stops before schema replacement.
- Existing rows stop the organization/currency migration with SQL error `51003`.
- Down migration with policy data stops with SQL error `51004`.
- Real local JWT smoke observed `Draft -> Active -> Cancelled` and two transition rows.
- Browser-observed one-click run returned `201`, `200`, `412`, `200`, `200` and two audit rows.
- Public HTTPS liveness, SQL readiness and demo page returned `200` on 2026-08-02.
- Azure SQL was read back as General Purpose Serverless with the free offer and `AutoPause` exhaustion behavior.
- App Service was read back as Linux `F1` / `Free`, .NET 10, HTTPS-only and TLS 1.2.
- The quota alert was read back as enabled on `free_amount_remaining <= 1000` every 15 minutes. Azure Free rejected only the manual test-notification call, so an email delivery has not been observed.
- Public-demo cleanup removed only expired rows in its configured organization during integration verification.
- Local demo database created and migrated successfully.
- EF Core reports no model changes pending a migration.
- NuGet reports no known vulnerable direct or transitive package from configured sources.
- `dotnet format --verify-no-changes` passes.
- Swagger publishes a Bearer security requirement, a quoted `If-Match` example and the actual JSON response media types.
- Local Markdown links resolve and no credential-like content was found.
- Final test cleanup left zero temporary SQL databases.

## 9. Open decisions

| ID | Decision | Impact | State |
|---|---|---|---|
| `OPEN-004` | Risk inputs, factors, result scale and wording | Blocks risk slice | Open |
| `OPEN-008` | External OpenID Connect issuer and customer identity lifecycle | Blocks public/customer authentication | Open |
| `OPEN-009` | Audit retention, export and support access | Blocks customer pilot | Open |
| `OPEN-010` | Measured capacity envelope | Blocks scalability claims | Open |
| `OPEN-011` | Customer hosting, licensed database, backup and restore target | Blocks customer pilot | Open |

## 10. Change history

| Version | Date | Change | Authority |
|---:|---|---|---|
| 0.6 | 2026-08-02 | Recorded the live zero-cost Azure deployment, alert boundary and public smoke evidence | Aitor Nain Mendoza Vallejo |
| 0.5 | 2026-08-02 | Added the approved one-click public-demo boundary, local evidence and Azure subscription blocker | Aitor Nain Mendoza Vallejo |
| 0.4 | 2026-08-02 | Clarified Swagger ETag input and aligned OpenAPI response media types | Aitor Nain Mendoza Vallejo |
| 0.3 | 2026-08-02 | Implemented lifecycle, organization security, concurrency, audit, MIT and local demo | Aitor Nain Mendoza Vallejo |
| 0.2 | 2026-08-01 | Product/architecture approvals and first slice | Aitor Nain Mendoza Vallejo |
| 0.1 | 2026-08-01 | Initial proposal based on the repository review | Not approved |
