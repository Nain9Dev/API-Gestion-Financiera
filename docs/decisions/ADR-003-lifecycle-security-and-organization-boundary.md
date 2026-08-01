# ADR-003: Policy lifecycle, concurrency and organization boundary

- Identifier: ADR-003
- Date: 2026-08-02
- Status: Approved
- Decision owner: Aitor Nain Mendoza Vallejo
- Supersedes: pending lifecycle, concurrency and security decisions in version 0.2 documentation

## Context

The first slice can create and query Draft policies, but an operational demonstration needs controlled state transitions, visible audit evidence and protection against stale or cross-organization updates. The solution must remain free for local development and avoid collecting personal data.

## Decision

### Currency

- `InsuredAmount` always has a required three-letter uppercase ISO 4217 alphabetic currency code.
- Domain validation enforces the alphabetic shape; a deployment allow-list determines which valid codes the API currently accepts.
- The local demo allow-list is `EUR`, `GBP` and `USD` and is configuration, not schema.
- Amount and currency are immutable after Draft creation in this slice.

### Activation completeness

`ActivatePolicy` requires:

- an opaque `InsuredPartyReference`, maximum 100 characters;
- `CoverageStartDate`;
- `CoverageEndDate`, equal to or later than the start date.

The reference belongs to an upstream system and must use synthetic data in the demo. The API does not store a person's name, document, address or contact details.

### Cancellation

- `Draft -> Cancelled` and `Active -> Cancelled` are allowed.
- A trimmed cancellation reason is required and limited to 500 characters.
- `Cancelled` is terminal.
- A repeated command is not silently accepted: a stale version returns `412`; a command using the current version against an invalid state returns `409`.

### Authentication and authorization

- All policy endpoints require JWT Bearer authentication.
- `PolicyReader` can read policies and transitions.
- `PolicyOperator` can read, create, activate and cancel.
- The immutable `sub` claim identifies the actor.
- A valid GUID `organization_id` claim defines the trusted organization scope.
- Policy identifiers outside the caller's organization return `404` to avoid disclosing existence.
- Local JWTs are issued only by `dotnet user-jwts`. A public or customer environment requires an approved external OpenID Connect issuer; the API never issues passwords or production tokens.

### Persistence and concurrency

- Every policy stores `OrganizationId`; normalized policy number uniqueness is per organization.
- SQL Server `rowversion` is exposed as a strong ETag.
- Mutation endpoints require `If-Match`; missing preconditions return `428`, malformed values return `400`, and stale values return `412` with `concurrency_conflict`.
- Every accepted activation or cancellation appends one transition containing organization, actor subject, previous/new status, UTC time, correlation identifier and optional cancellation reason.
- The policy update and transition insert share one EF Core `SaveChanges` transaction.

### Migration safety

The migration must not infer ownership or currency for existing rows. If any policy rows exist before the organization/currency migration, it stops with a stable SQL error before changing the schema. A real upgrade would require a separately reviewed backfill that identifies the exact organization and currency of every row.

## Consequences

- The demo proves real authentication, tenant isolation, optimistic concurrency and append-only API audit behavior.
- Organization scope can later use any standards-compatible issuer without changing domain or persistence contracts.
- The current API remains a modular monolith and does not add an identity server, tenant administration or personal-data store.
- `rowversion` intentionally couples the persistence implementation to SQL Server, already approved for this product.
- Existing databases containing rows require an explicit migration decision; silent defaults are rejected to preserve security and meaning.

## Rejected alternatives

- **Client-provided actor or organization:** rejected because request bodies are untrusted.
- **Global policy-number uniqueness:** rejected because organizations may legitimately use the same number.
- **Last-write-wins updates:** rejected because they can lose accepted state changes.
- **A custom password/token server:** rejected because authentication protocols should not be implemented locally.
- **Keycloak or distributed identity infrastructure for the demo:** rejected as operational complexity without current value.
- **Defaulting migrated rows to EUR or a demo organization:** rejected because it invents persisted business meaning and ownership.

## Reconsideration triggers

Reconsider when a customer identity provider, organization hierarchy, delegated administration, policy transfer, multi-currency amount conversion or non-SQL Server database becomes an approved requirement.

## Implementation evidence

- JWT reader/operator policies and trusted actor accessor are active in the API.
- Organization-scoped repository queries and composite SQL constraints are implemented.
- Activate, cancel and transition-history endpoints are implemented.
- SQL Server `rowversion`, ETag and `If-Match` return the approved conflict behavior.
- `OrganizationLifecycleSecurity` refuses unsafe forward/down migration with SQL errors `51003` and `51004`.
- Automated tests and a real local JWT smoke verify the complete lifecycle with synthetic data.

## Approval record

Approved by Aitor Nain Mendoza Vallejo on 2026-08-02 through explicit delegation to select the most useful, scalable and free-first decisions for the local technical demo.
