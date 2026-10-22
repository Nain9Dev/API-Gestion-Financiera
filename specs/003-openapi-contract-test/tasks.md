# Tasks 003 — OpenAPI contract evidence after the Scalar migration

- Document version: 0.1
- Date: 2026-10-22
- Labels: `[A]` agent completes, `[M]` mixed, `[H]` owner only

## Status

| ID | Task | Label | State |
|---|---|---|---|
| `T-030` | Point the integration test at `/openapi/v1.json`, rename it, assert `200` and the Bearer scheme | `[A]` | Done |
| `T-031` | Allow anonymous access to `/openapi/v1.json`, `/docs` and the `/swagger` redirect | `[A]` | Done |
| `T-032` | Port the deleted operation filter to `BearerSecurityDocumentTransformer` and `PolicyOperationTransformer` | `[A]` | Done |
| `T-033` | Run the full verification gate with SQL Server and record the result | `[A]` | Done |
| `T-034` | Update `docs/traceability.md` and `docs/changelog.md` | `[A]` | Done |
| `T-035` | Review and commit | `[H]` | Pending |

## Session handoff

- **Done and verified:** `T-030` to `T-034`. The renamed test fails with `401` before `T-031` and
  passes after `T-031` and `T-032`. Release build 0 warnings, `dotnet format` clean, full suite
  green with `POLICY_OPERATIONS_TEST_SQLSERVER` set (see `docs/changelog.md` 0.9 for counts).
- **Not verified:** the Azure deployment; nothing here was deployed.
- **Follow-up outside this spec:** `docs/architecture.md`, `docs/local-demo.md`, `README.md` and
  `docs/project-context.md` still describe Swashbuckle and the Swagger UI walkthrough; they were
  not updated in `ee39423` and are not touched here.
- **Blocked:** `T-035` waits for the owner.
