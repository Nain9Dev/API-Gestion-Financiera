# Plan 003 — OpenAPI contract evidence after the Scalar migration

- Document version: 0.1
- Status: Proposed
- Date: 2026-10-22
- Implements: `spec.md` in this directory

## Design decisions

### 1. Anonymous endpoints, not a weaker fallback policy

`MapOpenApi()`, `MapScalarApiReference("/docs")` and the `/swagger` redirect get
`.AllowAnonymous()` in `Program.cs`. The fallback policy (`RequireAuthenticatedUser`) stays as it
is, so every other endpoint keeps denying anonymous requests by default. This mirrors the previous
situation, where Swagger ran as middleware before authorization.

### 2. The deleted operation filter becomes two native transformers

`Microsoft.AspNetCore.OpenApi` exposes document and operation transformers with the same
`Microsoft.OpenApi` 2.x model the old Swashbuckle filter already used, so the logic ports almost
line by line into `PolicyOperations.Api/OpenApi/`:

- `BearerSecurityDocumentTransformer` (`IOpenApiDocumentTransformer`): registers the `Bearer`
  scheme under `components.securitySchemes` and adds the global security requirement.
- `PolicyOperationTransformer` (`IOpenApiOperationTransformer`): clears the requirement on
  `[AllowAnonymous]` operations, marks `If-Match` required with the quoted example and
  description, and rewrites response media types to `application/json` (`2xx`) or
  `application/problem+json` (errors).

Both are registered through `AddOpenApi(options => ...)`. No new package.

### 3. The test follows the document, keeps every assertion and adds the security ones

`SwaggerExplainsQuotedEtagAndDocumentsJsonResponses` is renamed to
`OpenApiDocumentExplainsQuotedEtagAndDocumentsJsonResponses`, requests `/openapi/v1.json` with an
anonymous client and asserts `200` explicitly. The existing assertions on `If-Match`, media types
and the anonymous demo operation stay unchanged. Two assertions are added for the Bearer scheme
and the global requirement, which the old test relied on implicitly through Swashbuckle
configuration.

## Observed native output before the change

Captured with `curl` against a local Release build after allowing anonymous access, before the
transformers existed:

| Element | Native generator | Required by `docs/` |
|---|---|---|
| `If-Match` on `activate` | optional, no description, no example | required, quoted example, description |
| `200` content | `application/json`, `text/json`, `text/plain` | `application/json` |
| `400` content | `application/json`, `text/json`, `text/plain` | `application/problem+json` |
| `components.securitySchemes` | absent | `Bearer` |
| document `security` | absent | one global requirement |

## Verification

```bash
dotnet build ./GestionFinanciera/GestionFinanciera.sln --configuration Release
dotnet format ./GestionFinanciera/GestionFinanciera.sln --verify-no-changes
dotnet test ./GestionFinanciera/GestionFinanciera.sln --configuration Release --no-build
```

with `POLICY_OPERATIONS_TEST_SQLSERVER` set, plus a manual `curl` of `/docs/` and
`/openapi/v1.json` on a local run for `REQ-031`.
