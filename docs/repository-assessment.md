# Repository assessment

- Document version: 0.1
- Status: Historical evidence
- Assessment date: 2026-08-01
- Assessed commit: `2ba8c40`
- Scope: Local checkout, tracked code, README, portfolio description and build

## Conclusion

El repositorio es un prototipo compilable, no una API financiera completa ni una implementación de Clean Architecture. Su activo útil es una base mínima de ASP.NET Core, EF Core y SQL Server alrededor de `Policy`; el riesgo principal es seguir construyendo sin resolver la contradicción de producto.

> Historical note: this document describes commit `2ba8c40` before the approved .NET 10 modular migration. Use the current README and project context for present behavior.

## Verified inventory

| Area | Verified state |
|---|---|
| Solution | One solution with one ASP.NET Core project |
| Runtime target | `net8.0` |
| Packages | EF Core SQL Server/Design/Tools 8.0.24; Swashbuckle 6.6.2 |
| Domain code | One `Policy` class |
| API | `GET /api/policies`; `POST /api/policies` |
| Persistence | One `FinancialDbContext`; one initial migration; `Policies` table |
| Automated tests | None |
| Authentication | None configured |
| Authorization | Middleware is present, but no authentication scheme or protected endpoint exists |
| Public demo | None declared in repository |
| Repository license | Missing |

## Verification performed

- `dotnet build GestionFinanciera/GestionFinanciera.sln --configuration Release`
- Result: success, 0 warnings, 0 errors.
- Build environment: .NET SDK 10.0.302; project target `net8.0`.

Not verified:

- API startup;
- SQL Server connectivity;
- migration application or recovery;
- endpoint responses against a real database;
- tests, because no test project exists;
- security, performance, concurrency or deployment.

## Contract and product contradictions

| Source | Claim | Evidence conflict |
|---|---|---|
| Previous README | Personal financial management with categories, income, expenses and budgets | No related entity, endpoint, table or rule exists |
| Previous README | Clean Architecture with Domain/Application/Infrastructure/Presentation | Only one project; controller depends directly on `DbContext`; `Application` is empty |
| Portfolio | Policy management and risk calculation with hexagonal architecture | Policy creation/listing exist; no risk calculation and no ports/adapters structure exists |
| `.http` file | Calls `/weatherforecast/` | No weather endpoint exists |

## Implemented business behavior

- `PolicyNumber` cannot be null, empty or whitespace in the constructor.
- `InsuredAmount` must be greater than zero in the constructor.
- A new policy receives a new `Guid`, becomes active and uses local server time for `IssueDate`.
- `CancelPolicy` sets `IsActive` to false, but no API flow invokes it.
- SQL mapping limits `PolicyNumber` to 50 characters and stores `InsuredAmount` as `decimal(18,2)`.

No uniqueness, lifecycle transition, ownership, audit, risk or concurrency rule is implemented.

## Material technical findings

| Priority | Finding | Impact | Required response |
|---|---|---|---|
| Gate | Product identity is inconsistent | Any new domain work may be discarded | Approve or change `ADR-001` first |
| High before exposure | API returns `Exception.Message` in a public 500 response | Leaks internal technical details | Replace with sanitized Problem Details and internal logging in an authorized implementation |
| High before exposure | No authentication or protected endpoint | Any deployed data would be public | Define trust boundary and authorization before public deployment |
| Medium | `CreatedAtAction` points to a collection action, not a get-by-id resource | `Location` does not represent the created resource contract | Add an explicit get-by-id contract when approved |
| Medium | `DateTime.Now` is persisted | Ambiguous timezone and inconsistent distributed behavior | Adopt UTC timestamps in the target contract |
| Medium | No unique constraint for `PolicyNumber` | Duplicate business identifiers can be stored | Decide uniqueness scope, then enforce in domain and SQL Server |
| Medium | List endpoint is unbounded and unordered | Poor capacity and unstable results | Define pagination and stable ordering |
| Medium | Controller owns business construction and persistence | Domain/application orchestration is coupled to HTTP and EF Core | Move use cases behind application boundaries if `ADR-002` is approved |
| Medium | Machine-specific connection string is tracked | Non-portable configuration and local identifier disclosure | Move environment values out of tracked settings |
| Medium | No tests | Existing behavior cannot be changed safely | Add focused domain and SQL Server integration tests with the first slice |
| Low | Package/runtime baseline is near end of support | .NET 8 support ends 2026-11-10 | Approve .NET 10 migration before expanding implementation |

## What should be preserved until changed explicitly

- `GET /api/policies` and `POST /api/policies` are the only current public routes.
- JSON uses the serializer defaults currently produced by ASP.NET Core.
- SQL table and column names in the initial migration are existing persistence contracts.
- Existing consumer usage is unknown; any breaking change needs an explicit replacement or versioning decision.

## Recommended next action

Approve the product and architecture ADRs, then implement one vertical slice that preserves or explicitly replaces the existing create/list behavior while adding domain tests, get-by-id, safe errors and SQL Server integration verification. Do not add risk scoring until its inputs, outputs and non-advisory boundary are approved.
