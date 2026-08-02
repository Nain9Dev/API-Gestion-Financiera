# Documentation map

- Document version: 0.5
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
| `README.md` | Public presentation and short status | 0.5 | Active | Version 0.4 |
| `docs/project-context.md` | Identity, scope, authority, readiness and sources | 0.5 | Active | Version 0.4 |
| `docs/business-rules.md` | Implemented behavior and approved rules | 0.5 | Active | Version 0.4 |
| `docs/architecture.md` | Current architecture and technical decisions | 0.5 | Approved | Version 0.4 |
| `docs/roadmap.md` | Gates, Definition of Done and work order | 0.5 | Active | Version 0.4 |
| `docs/economic-brief.md` | Commercial hypotheses, limits and validation | 0.5 | Draft | Version 0.4 |
| `docs/local-demo.md` | Local visual walkthrough with synthetic data | 0.2 | Active | Version 0.1 |
| `docs/public-demo.md` | One-click scenario, deployment and operating boundary | 0.1 | Implemented locally; cloud blocked | None |
| `docs/repository-assessment.md` | Review of the previous prototype | 0.1 | Historical evidence | None |
| `docs/decisions/ADR-001-product-direction.md` | Product direction | 0.1 | Approved | None |
| `docs/decisions/ADR-002-runtime-and-architecture.md` | Runtime and solution structure | 0.1 | Approved | None |
| `docs/decisions/ADR-003-lifecycle-security-and-organization-boundary.md` | Lifecycle, concurrency, authentication and organization | 0.1 | Approved | Pending decisions in version 0.2 docs |
| `docs/decisions/ADR-004-repository-license.md` | Repository license | 0.1 | Approved | None |
| `docs/decisions/ADR-005-public-demo-and-free-hosting.md` | Public demo and free hosting | 0.1 | Approved | Internet deployment pending subscription |

## Status meanings

- `Active evidence`: dated, verified observation that may become stale.
- `Draft`: incomplete and not authoritative for investment or implementation.
- `Proposed`: a specific decision waiting for approval.
- `Approved`: authorized by the named decision owner.
- `Superseded`: retained only for historical traceability.

## Approval required after the public-demo slice

ADR-005 approves the isolated synthetic public demo and its zero-cash hosting target. It does not approve customer identity, real personal data, paid infrastructure or a customer pilot. The inspected Azure accounts currently have no active subscription, so no public backend has been created.

Approval must record the date, decision owner and affected version. A document existing in this directory does not make it approved.
