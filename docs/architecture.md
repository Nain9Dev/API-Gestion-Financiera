# Architecture

- Document version: 0.5
- Status: Approved and implemented for the local and public-demo boundaries
- Date: 2026-08-02
- Governing ADRs: ADR-002, ADR-003 and ADR-005

## Decision

Use one .NET 10 modular monolith and one SQL Server database. Keep business rules independent from ASP.NET Core and EF Core. Use standards-compatible JWT validation at the API boundary and organization-scoped persistence. Add distributed components only after a measured requirement.

## Structure

```text
JWT client / Swagger                 Anonymous portfolio visitor
    -> PolicyOperations.Api
        <- fixed /api/v1/demo/run + same-origin /demo page
        -> PolicyOperations.Application
            -> PolicyOperations.Domain
        -> PolicyOperations.Infrastructure
            -> EF Core
                -> SQL Server

PolicyOperations.Domain.Tests -> Domain
PolicyOperations.IntegrationTests -> API + Infrastructure + SQL Server
```

| Component | Responsibility | Explicit exclusion |
|---|---|---|
| Domain | Policy invariants, lifecycle and transition creation | HTTP, EF Core, SQL and JWT parsing |
| Application | Organization-scoped use cases, currency catalog port, concurrency orchestration and DTOs | Controllers and SQL implementation |
| Infrastructure | EF Core context, mappings, migrations and concrete queries | Authentication and business decisions |
| API | Composition, JWT authorization, trusted actor context, ETags, OpenAPI, fixed public-demo façade and safe errors | Arbitrary anonymous data and client-selected organization |

## Dependency decisions

- No MediatR, AutoMapper, generic repository, validation framework or result framework.
- `IPolicyRepository` exposes only policy-specific queries and concurrency behavior.
- `IUnitOfWork` keeps policy update and transition insert in one `SaveChanges` transaction.
- `ICurrencyCatalog` separates stable ISO-shaped domain syntax from a deployment allow-list.
- Built-in `TimeProvider` supplies UTC time.
- Microsoft JWT Bearer validates tokens; the application never implements password or token issuance.
- `dotnet user-jwts` is used only as the local issuer.
- SQL constraints remain authoritative under concurrent or non-API writes.
- The public façade reuses `PolicyService`; it does not duplicate lifecycle rules.

## Technology baseline

| Responsibility | Version | State |
|---|---:|---|
| SDK feature band | 10.0.300 with latest-patch roll-forward | Pinned |
| Runtime/API | .NET and ASP.NET Core 10 | Implemented |
| Authentication | Microsoft.AspNetCore.Authentication.JwtBearer 10.0.10 | Implemented, MIT |
| Persistence | EF Core SQL Server 10.0.10 | Implemented |
| OpenAPI UI | Swashbuckle 10.2.3 | Implemented, MIT |
| Unit tests | xUnit 2.9.3 and runner 3.1.5 | Implemented |
| Test platform | Microsoft.NET.Test.Sdk 18.8.1 | Implemented |
| Local database | SQL Server 2025 Standard Developer 17.0.4065.4 | Verified, development/test only |

Official sources checked 2026-08-02:

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [Local JWT generation with dotnet user-jwts](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn?view=aspnetcore-10.0)
- [EF Core optimistic concurrency and SQL Server rowversion](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [ISO 4217 currency codes](https://www.iso.org/iso-4217-currency-codes.html)
- [JWT Bearer 10.0.10 package](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.10)
- [MIT License](https://opensource.org/license/MIT)
- [SQL Server 2025 editions](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025)

## Trust and organization boundary

```text
Validated JWT
  sub --------------------> audit actor
  organization_id (GUID) -> query and persistence scope
  role -------------------> read/write authorization
```

- A fallback policy denies anonymous access to future endpoints by default.
- `PolicyReader` reads; `PolicyOperator` reads and mutates.
- API contracts do not accept organization or actor fields.
- Every repository method requires `OrganizationId` for policies, counts and transitions.
- Composite uniqueness is `(OrganizationId, NormalizedPolicyNumber)`.
- A composite foreign key prevents an audit row from referencing a policy in another organization.
- Cross-organization lookup returns `404` rather than confirming existence.

The API can later trust an external OpenID Connect issuer without changing domain or SQL ownership. Issuer selection, user lifecycle and organization administration are not implemented.

The only anonymous business operation is the optional public-demo run. It has no request body, assigns a dedicated organization and actor on the server and cannot authorize access to `/api/v1/policies`.

## Domain and persistence

- `PolicyNumber` is trimmed and invariant-upper normalized.
- `InsuredAmount` uses `decimal(18,2)` and always has a configured three-letter currency.
- Draft activation stores an opaque insured-party reference and `date` coverage boundaries.
- Status is a constrained readable string: `Draft`, `Active` or `Cancelled`.
- `CreatedAtUtc` and transition times use `datetimeoffset(7)`.
- `Policy.Version` maps to SQL Server `rowversion`.
- `PolicyTransitions` is append-only through the public API.
- SQL checks independently protect positive amount, currency shape, status and Active completeness.

## Concurrency contract

Create, get and mutation responses return a base64 `version` plus an equivalent strong `ETag` response header. Activate and cancel require `If-Match`.

OpenAPI marks `If-Match` as required, shows a quoted example that can be pasted into Swagger, and documents normal responses as `application/json` and errors as `application/problem+json`.

The application rejects an already stale token before mutation. EF Core still places the original `rowversion` in the SQL `UPDATE` predicate, protecting the race between read and save. A database mismatch becomes `412 concurrency_conflict`; no automatic merge or retry is performed.

## Transaction ownership and audit

`PolicyService` owns each lifecycle command:

1. load one policy inside the trusted organization;
2. verify the expected version;
3. execute the domain transition;
4. add the returned audit transition;
5. call one Unit of Work save.

EF Core's relational save transaction commits or rolls back the policy update and audit insert together. Tests verify that rejected completeness and stale-version commands leave no transition.

## Migration strategy

Migration history is preserved:

1. `InitialCreate` — historical schema;
2. `PolicyLifecycleFoundation` — normalized number, explicit status and UTC timestamp;
3. `OrganizationLifecycleSecurity` — organization, currency, activation data, rowversion and transition table.

The third migration refuses any existing policy rows with SQL error `51003`; it cannot infer ownership or currency. Its down migration refuses rows with `51004` because dropping the new fields would lose business/security data. A customer upgrade requires an explicit previewed backfill and roll-forward/restore plan.

Migration commands require `POLICY_OPERATIONS_MIGRATIONS_SQLSERVER`; there is no fallback connection.

## API and error boundary

- Contract root is `/api/v1`.
- Problem Details returns stable English `code` and `traceId`.
- Authorization failures return sanitized `401`/`403` bodies.
- Domain validation uses `400`, missing scoped resources `404`, state/number conflicts `409`, stale version `412`, and missing precondition `428`.
- Unexpected errors are logged internally and return `internal_error` without exception details.
- JSON enum values and public identifiers remain English.
- The public-demo page is same-origin, sends no credentials and receives no-store responses.
- A fixed-window rate limiter returns `429 public_demo_rate_limited` before demo work starts.
- Expired demo data is pruned inside a transaction and filtered by the dedicated organization.
- `/health` remains liveness while `/health/ready` checks SQL Server.

## Quality evidence

- Release build: 0 warnings and 0 errors.
- 22 domain tests.
- 17 API tests covering authentication, roles, scope, lifecycle, public demo, readiness, concurrency and audit.
- 4 real SQL migration tests covering preservation and forward/down refusal.
- 43 total tests; 21 use SQL Server 2025.
- Real local JWT smoke: create Draft, activate, cancel and read two transitions.
- Real browser smoke: one click produced the expected five-step result and two transitions.
- Test database deletion is limited by explicit prefixes.

## Capacity and deployment boundary

The earlier 100,000-policy and 10-concurrent-client target remains an unverified proposal. No scalability or availability claim is authorized until a repeatable report records hardware, data distribution, query plans, latency and error rate.

| Phase | Deployment | State |
|---|---|---|
| Local technical demo | Local API, local JWT and SQL Server Standard Developer | Verified |
| Public portfolio demo | Azure App Service F1 and Azure SQL Free, synthetic fixed scenario | Implemented locally; cloud blocked by inactive subscription |
| Portfolio evidence | Public source, CI, local browser evidence and later live link | In progress |
| Customer pilot | External issuer, licensed database/hosting, backup and privacy decisions | Not authorized |
| Production | Measured availability, restore, monitoring and support | Not ready; not guaranteed free |

The owner's workstation and Standard Developer edition must not be used as customer production infrastructure.
