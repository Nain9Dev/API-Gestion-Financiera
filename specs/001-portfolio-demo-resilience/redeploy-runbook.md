# Runbook — redeploy the API to Azure (T-019 / T-116)

- Date: 2026-10-26
- Scope: publish the current `main`-line build of `PolicyOperations.Api` to the existing App Service.
- Cost: 0 EUR. No resource is created, resized or reconfigured.

## Verified facts (read-only `az` queries, 2026-10-26)

| Item | Value |
|---|---|
| Subscription | `Azure subscription 1` (enabled) |
| Resource group / app | `rg-nain-policy-demo` / `nain-policy-demo-api` |
| Runtime | Linux, `DOTNETCORE\|10.0`, HTTPS only |
| Build on deploy | `SCM_DO_BUILD_DURING_DEPLOYMENT=false`, `ENABLE_ORYX_BUILD=false` (upload a prebuilt publish output) |
| Migrations | `Database__ApplyMigrationsOnStartup=false`; no migration changed since 2026-08-02, so none to apply |
| Public demo | `PublicDemo__Enabled=true` |
| Connection string | `DefaultConnection` (SQLAzure) already set; do not touch |
| Deployed behaviour | Pre-ADR-006 build: no CORS for `https://policy.naindev.com` (preflight returns 401) |

## Steps

| # | Step | Label | Verify |
|---|---|---|---|
| 1 | Work from a clean checkout of the branch to ship; run the gate in `AGENTS.md` section 8 with `POLICY_OPERATIONS_TEST_SQLSERVER` set | `[A]` | 0 warnings, 0 skipped suites |
| 2 | `dotnet publish GestionFinanciera/PolicyOperations.Api -c Release -o <scratch>/publish` | `[A]` | `PolicyOperations.Api.dll` and `wwwroot/demo` present |
| 3 | Zip the contents of `publish` (files at the zip root) | `[A]` | `PolicyOperations.Api.dll` at the root of the zip |
| 4 | Confirm the target and the logged-in account: `az account show` | `[H]` | Subscription `Azure subscription 1` |
| 5 | `az webapp deploy -g rg-nain-policy-demo -n nain-policy-demo-api --type zip --src-path <zip>` | `[H]` | Command reports success |
| 6 | Wait for the restart, then `GET /health` and `GET /health/ready` | `[A]` | 200 / 200 |
| 7 | Preflight: `OPTIONS /api/v1/demo/run` with `Origin: https://policy.naindev.com` and `Access-Control-Request-Method: POST` | `[A]` | 204 with `Access-Control-Allow-Origin: https://policy.naindev.com` |
| 8 | Cold smoke test: wait about 30 minutes idle, then one `POST /api/v1/demo/run` | `[A]` | 200 or `503` with `Retry-After`, never `500`; record elapsed time |
| 9 | Open `https://policy.naindev.com/`, run the scenario in a browser | `[H]` | Source label reads `Live · SQL Server` |
| 10 | Record the evidence in `docs/public-demo.md`, `docs/changelog.md` and mark `T-019` / `T-116` done | `[A]` | Documents match observed numbers |

## Rollback

`az webapp deployment list-publishing-credentials` is not needed. If step 6 fails, redeploy the previous
build from the last known-good commit with the same command; the database is untouched.

## Quota caution

Each live run spends free vCore-seconds. Steps 6 to 9 total fewer than ten requests; do not loop them.
