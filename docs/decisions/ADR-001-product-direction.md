# ADR-001: Product direction

- Date: 2026-08-01
- Status: Approved
- Decision owner: Aitor Nain Mendoza Vallejo

## Context

The repository name and previous README describe financial management, while the implementation and public portfolio center on insurance policies. No market evidence supports either direction. Continuing without a decision would mix unrelated domains and produce misleading portfolio claims.

## Decision

Use this repository for a portfolio-first **B2B Insurance Policy Operations API** that demonstrates policy lifecycle, auditable state transitions, deterministic risk assessment and SQL Server persistence using synthetic data.

Treat commercial usefulness as a hypothesis. Validate the problem and buyer before building integrations, multi-tenancy, billing or production operations.

## Current problem solved

- removes the contradiction between implemented `Policy` code and the intended public story;
- creates a bounded domain in which C#, API design, DDD, SQL Server, testing, security and observability can be demonstrated honestly;
- prevents expansion into a generic financial platform without evidence.

## Consequences

Benefits:

- most current code remains relevant as a starting point;
- domain invariants and lifecycle offer meaningful business logic;
- portfolio evidence can be tied to executable tests and SQL behavior.

Costs and risks:

- the repository name remains historically inconsistent until a separate rename decision;
- insurance is a sensitive domain and must avoid claims of real underwriting, legal compliance or financial advice;
- business rules require validation with actual domain participants;
- portfolio wording must remain conservative until implementation exists.

## Rejected alternatives

- **Personal finance API:** rejected for now because no income, expense, category or budget code exists and the public portfolio already describes insurance.
- **Generic financial platform:** rejected because it lacks a defined buyer and would encourage speculative abstractions.
- **Full insurance SaaS:** rejected because multi-tenancy, integrations, personal data, regulation, support and hosting exceed the evidence and current gate.
- **Continue adding endpoints without product approval:** rejected because it would deepen the current inconsistency.

## Affected sources and components

- README and portfolio description;
- domain naming and business rules;
- API contracts and database schema after implementation authorization;
- architecture and commercial validation plan.

## Migration impact

The pre-1.0 routes were replaced by the explicit `/api/v1/policies` contract. The existing `Policies` table and initial migration history were preserved through a versioned data-preserving migration.

## Reconsideration trigger

- direct interviews show a different repeated problem or buyer;
- the user chooses portfolio-only financial management despite discarding current policy code;
- legal or operational constraints make even a synthetic insurance demo misleading or unsafe.

## Approval record

Approved by Aitor Nain Mendoza Vallejo on 2026-08-01. Approval covered the B2B insurance operations direction and implementation of the migration-first plan. Risk-assessment details remain a separate pending decision.
