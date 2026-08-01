# ADR-002: Runtime and architecture baseline

- Date: 2026-08-01
- Status: Approved and implemented for the first slice
- Decision owner: Aitor Nain Mendoza Vallejo

## Context

The current project targets .NET 8 and places controllers, domain and EF Core in one project. .NET 8 support ends on 2026-11-10. The repository claims Clean/Hexagonal Architecture but does not implement those boundaries.

## Decision

Before expanding product behavior:

1. migrate to the current supported patch of .NET 10 LTS;
2. organize the solution as a modular monolith with Domain, Application, Infrastructure and API projects;
3. retain one deployable API and one SQL Server database;
4. use plain application services/use-case handlers and add abstractions only for real external boundaries;
5. keep SQL Server as the integration-test provider for persistence-specific behavior;
6. avoid microservices, messaging, caching and generic frameworks until measured requirements justify them.

The implemented project names are `PolicyOperations.Domain`, `PolicyOperations.Application`, `PolicyOperations.Infrastructure` and `PolicyOperations.Api`. The replacement API contract starts at `/api/v1`.

## Problem solved now

- aligns future work with a supported LTS horizon through 2028-11-14;
- makes architecture claims demonstrable in code;
- isolates business rules from HTTP and EF Core;
- enables focused unit and integration testing without distributed complexity.

## Cost and risk

- package compatibility and OpenAPI tooling must be reviewed;
- project and namespace moves increase diff size and can break consumers;
- migrations must be verified against SQL Server after package upgrade;
- a four-project solution is only justified if boundaries remain small and behavior-driven.

## Rejected alternatives

- **Stay on .NET 8 for new development:** rejected because support ends shortly after this assessment.
- **Microservices:** rejected because there is one bounded product slice and no measured independent scaling or deployment need.
- **Single project indefinitely:** rejected for the proposed portfolio objective because current direct coupling prevents honest demonstration of the claimed boundaries.
- **CQRS framework, mediator and generic repositories by default:** rejected because they add indirection without a confirmed variation or integration boundary.
- **In-memory persistence tests only:** rejected because they cannot validate SQL Server constraints, transactions, concurrency or generated queries.

## Adoption plan

1. add characterization tests for current domain and API behavior;
2. upgrade target framework and aligned packages;
3. verify Release build and current contract;
4. create the approved project boundaries;
5. move one create/get vertical slice while preserving or explicitly versioning routes;
6. run real SQL Server integration tests and migration verification;
7. update architecture and portfolio evidence to match observed behavior.

The initial adoption was authorized and implemented on 2026-08-01. Future capabilities remain governed by the roadmap and business-rule approvals.

## Reconsideration trigger

- an approved consumer requires a different supported runtime;
- measured deployment or team boundaries justify independent services;
- the product direction is rejected and the repository is archived or repurposed.

## Implementation evidence

- .NET 10.0.10 aligned packages and a pinned .NET 10 SDK feature band;
- strict Release build with zero warnings and zero errors;
- four modular production projects and two test projects;
- EF Core migrations retained in Infrastructure;
- real SQL Server 2025 API and migration tests.

## Approval record

Approved by Aitor Nain Mendoza Vallejo on 2026-08-01.
