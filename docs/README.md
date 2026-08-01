# Documentation map

- Document version: 0.4
- Status: Active
- Date: 2026-08-02
- Owner: Aitor Nain Mendoza Vallejo

## Purpose

This directory separates current behavior, approved decisions and future work. A proposal is not an approved rule until the decision owner records that approval.

## Authority order

1. Versioned code, migrations and configuration describe current implemented behavior.
2. `project-context.md` is the entry point for scope, status and readiness gates.
3. `business-rules.md` defines approved rules and invariants.
4. Approved ADRs govern architectural decisions within their stated scope.
5. `roadmap.md` orders the work; it does not approve implementation by itself.
6. `repository-assessment.md` is a dated review of the previous prototype, not a current contract.
7. `economic-brief.md` records spending limits and commercial experiments.

If two approved sources conflict and neither has precedence, the affected work stops until the conflict is resolved.

## Canonical source registry

| Source | Scope | Version | Status | Replaces |
|---|---|---:|---|---|
| `README.md` | Public presentation and short status | 0.4 | Active | Version 0.3 |
| `docs/project-context.md` | Identity, scope, authority, readiness and sources | 0.4 | Active | Version 0.3 |
| `docs/business-rules.md` | Implemented behavior and approved rules | 0.4 | Active | Version 0.3 |
| `docs/architecture.md` | Current architecture and technical decisions | 0.4 | Approved | Version 0.3 |
| `docs/roadmap.md` | Gates, Definition of Done and work order | 0.4 | Active | Version 0.3 |
| `docs/economic-brief.md` | Commercial hypotheses, limits and validation | 0.4 | Draft | Version 0.3 |
| `docs/local-demo.md` | Local visual walkthrough with synthetic data | 0.2 | Active | Version 0.1 |
| `docs/repository-assessment.md` | Review of the previous prototype | 0.1 | Historical evidence | None |
| `docs/decisions/ADR-001-product-direction.md` | Product direction | 0.1 | Approved | None |
| `docs/decisions/ADR-002-runtime-and-architecture.md` | Runtime and solution structure | 0.1 | Approved | None |
| `docs/decisions/ADR-003-lifecycle-security-and-organization-boundary.md` | Lifecycle, concurrency, authentication and organization | 0.1 | Approved | Pending decisions in version 0.2 docs |
| `docs/decisions/ADR-004-repository-license.md` | Repository license | 0.1 | Approved | None |

## Status meanings

- `Active evidence`: dated, verified observation that may become stale.
- `Draft`: incomplete and not authoritative for investment or implementation.
- `Proposed`: a specific decision waiting for approval.
- `Approved`: authorized by the named decision owner.
- `Superseded`: retained only for historical traceability.

## Approval required after the lifecycle slice

ADR-003 and ADR-004 approve the lifecycle/security contract and repository license. Risk-assessment inputs and any production identity, hosting or personal-data processing remain unapproved.

Approval must record the date, decision owner and affected version. A document existing in this directory does not make it approved.
