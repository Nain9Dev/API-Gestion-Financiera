# Project context

- Document version: 0.4
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
| Current milestone | Local technical demo | Passed |
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
| Local technical demo | Passed | JWT smoke, lifecycle, concurrency and audit verified with synthetic data | Keep local-only boundary |
| Portfolio publication evidence | In progress | README and demo guide updated | CI and recorded/screenshotted walkthrough optional next evidence |
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
- production hosting, backup, restore, alerting and support procedures.

These are not defects in the local technical demo. They remain separate decisions or later gates.

## 6. Data and security boundaries

- Requests are untrusted and server validation is authoritative.
- JWT actor and organization claims are the only scope source.
- Connection strings and signing keys remain outside source control.
- Local tokens use `dotnet user-jwts` and must not be accepted from the internet.
- The `/health` endpoint is anonymous process liveness, not SQL readiness.
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

No paid service, credit card or metered production resource is authorized.

## 8. Current verification

- Release build: 0 warnings, 0 errors.
- Domain tests: 22 passed.
- SQL Server API/security/OpenAPI tests: 14 passed.
- SQL Server migration tests: 4 passed.
- Total: 40 passed; 18 use real SQL Server.
- Legacy active/cancelled rows and timestamps remain preserved by the lifecycle migration.
- Duplicate legacy normalization stops before schema replacement.
- Existing rows stop the organization/currency migration with SQL error `51003`.
- Down migration with policy data stops with SQL error `51004`.
- Real local JWT smoke observed `Draft -> Active -> Cancelled` and two transition rows.
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
| `OPEN-011` | Hosting, licensed production database, backup and restore target | Blocks customer pilot | Open |
| `OPEN-012` | Portfolio publication/recording timing | Blocks public demo evidence only | Open |

## 10. Change history

| Version | Date | Change | Authority |
|---:|---|---|---|
| 0.4 | 2026-08-02 | Clarified Swagger ETag input and aligned OpenAPI response media types | Aitor Nain Mendoza Vallejo |
| 0.3 | 2026-08-02 | Implemented lifecycle, organization security, concurrency, audit, MIT and local demo | Aitor Nain Mendoza Vallejo |
| 0.2 | 2026-08-01 | Product/architecture approvals and first slice | Aitor Nain Mendoza Vallejo |
| 0.1 | 2026-08-01 | Initial proposal based on the repository review | Not approved |
