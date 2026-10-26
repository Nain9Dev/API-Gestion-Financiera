# Documentation map

- Document version: 0.8
- Status: Active
- Date: 2026-10-22
- Owner: Aitor Nain Mendoza Vallejo

## Purpose

This directory separates current behavior, approved decisions and future work. A proposal is not an approved rule until the decision owner records that approval.

## Authority order

1. Versioned code, migrations and configuration describe current implemented behavior.
2. `project-context.md` is the entry point for scope, status and readiness gates.
3. `business-rules.md` defines approved rules and invariants.
4. Approved ADRs govern architectural decisions within their stated scope.
5. `specs/NNN-*/` describes the change currently being built; its requirements move into
   `business-rules.md` when the owner approves them.
6. `traceability.md` maps each approved rule to the test that demonstrates it.
7. `roadmap.md` orders the work; it does not approve implementation by itself.
8. `changelog.md` records what changed and when; it approves nothing.
9. `repository-assessment.md` is a dated review of the previous prototype, not a current contract.
10. `economic-brief.md` records spending limits and commercial experiments.

`AGENTS.md` in the repository root is the canonical constitution for any agent or contributor
working here. `CLAUDE.md` only imports it and adds Claude Code specifics.

If two approved sources conflict and neither has precedence, the affected work stops until the conflict is resolved.

## Canonical source registry

| Source | Scope | Version | Status | Replaces |
|---|---|---:|---|---|
| `README.md` | Public presentation and short status | 0.6 | Active | Version 0.5 |
| `docs/project-context.md` | Identity, scope, authority, readiness and sources | 0.6 | Active | Version 0.5 |
| `docs/business-rules.md` | Implemented behavior and approved rules | 0.5 | Active | Version 0.4 |
| `docs/architecture.md` | Current architecture and technical decisions | 0.6 | Approved | Version 0.5 |
| `docs/roadmap.md` | Gates, Definition of Done and work order | 0.6 | Active | Version 0.5 |
| `docs/economic-brief.md` | Commercial hypotheses, limits and validation | 0.6 | Draft | Version 0.5 |
| `docs/local-demo.md` | Local visual walkthrough with synthetic data | 0.2 | Active | Version 0.1 |
| `docs/public-demo.md` | One-click scenario, deployment and operating boundary | 0.2 | Active evidence | Version 0.1 |
| `docs/traceability.md` | Approved rule to demonstrating test | 0.1 | Active | None |
| `docs/changelog.md` | Dated history of behavioural changes | 0.1 | Active | None |
| `docs/repository-assessment.md` | Review of the previous prototype | 0.1 | Historical evidence | None |
| `docs/decisions/ADR-001-product-direction.md` | Product direction | 0.1 | Approved | None |
| `docs/decisions/ADR-002-runtime-and-architecture.md` | Runtime and solution structure | 0.1 | Approved | None |
| `docs/decisions/ADR-003-lifecycle-security-and-organization-boundary.md` | Lifecycle, concurrency, authentication and organization | 0.1 | Approved | Pending decisions in version 0.2 docs |
| `docs/decisions/ADR-004-repository-license.md` | Repository license | 0.1 | Approved | None |
| `docs/decisions/ADR-005-public-demo-and-free-hosting.md` | Public demo and free hosting | 0.1 | Approved and implemented | None |
| `docs/decisions/ADR-006-free-tier-resilience-and-demo-evidence.md` | Free-tier resilience, demo evidence and CI proof | 0.1 | Proposed | None |
| `specs/001-portfolio-demo-resilience/` | In-flight change implementing ADR-006 | 0.1 | Proposed | None |
| `docs/decisions/ADR-007-blueprint-first-client-presentation.md` | Blueprint-first client, honest execution source, brand tokens | 0.1 | Proposed | None |
| `specs/002-live-architecture-blueprint/` | In-flight change implementing ADR-007 | 0.1 | Proposed | None |
| `specs/003-openapi-contract-test/` | OpenAPI contract evidence restored after the Scalar migration | 0.1 | Proposed | None |
| `specs/004-prewarm-live-demo/` | Client wakes the free-tier database on load and bounds a live run to 6 s (partly amended by 005) | 0.1 | Proposed | None |
| `specs/005-intent-warmup-cost-guard/` | Wake the database only on visitor intent, simulate while it wakes, replay in about 1 s, Azure cost-guard runbook | 0.1 | Proposed | None |
| `AGENTS.md` | Canonical constitution for agents and contributors | 0.1 | Active | None |
| `SECURITY.md` | Vulnerability reporting and security posture | 0.1 | Active | None |

## Status meanings

- `Active evidence`: dated, verified observation that may become stale.
- `Draft`: incomplete and not authoritative for investment or implementation.
- `Proposed`: a specific decision waiting for approval.
- `Approved`: authorized by the named decision owner.
- `Superseded`: retained only for historical traceability.

## Approval required after the public-demo slice

ADR-005 approves the isolated synthetic public demo and its zero-cash hosting target. That deployment is now live and verified. The decision still does not approve customer identity, real personal data, paid infrastructure or a customer pilot.

Approval must record the date, decision owner and affected version. A document existing in this directory does not make it approved.

## Open approval: ADR-006

ADR-006 is `Proposed` and its code is implemented but not deployed. It fixes a measured `500` on
the live public demo caused by the Azure SQL free-tier resume, and adds `503 service_unavailable`
to the demo and policy contracts. Until the owner approves it:

- the rules it introduces are listed as `Proposed` in `business-rules.md`;
- the fix is not released, so the defect remains reproducible in production.

Rules marked `Proposed` in `business-rules.md` while their code is already merged are recorded as
such deliberately. That state is the honest one, and it is resolved by an approval decision, not by
editing the status.
