# Public portfolio demo

- Document version: 0.2
- Status: Active evidence
- Date: 2026-08-02
- Governing decision: ADR-005

## Live result

Open [https://nain-policy-demo-api.azurewebsites.net/demo/](https://nain-policy-demo-api.azurewebsites.net/demo/) and select **Run demo**. The page uses the deployed API and Azure SQL database; it does not simulate the result in the browser.

The deployment smoke test observed:

1. `201` — Draft created;
2. `200` — policy activated;
3. `412 concurrency_conflict` — stale version rejected;
4. `200` — current version cancelled;
5. `200` — two audit transitions returned.

The final policy status was `Cancelled`. `/health`, `/health/ready` and `/demo/` also returned `200` over HTTPS.

F1 and Azure SQL Serverless can sleep or start slowly. The first request after a quiet period may take longer than later requests.

## Public contract

### Run the fixed scenario

- Method: `POST`
- URL: `https://nain-policy-demo-api.azurewebsites.net/api/v1/demo/run`
- Required headers: `Accept: application/json`
- Request body: none
- Authentication: none
- Observed status: `200 OK`

A request body is rejected with `400 Bad Request` and stable code `public_demo_body_not_allowed`.

Representative response:

```json
{
  "runId": "06C3575F",
  "dataRetentionHours": 24,
  "steps": [
    {
      "operation": "create_draft",
      "status": 201,
      "result": "succeeded",
      "resourceStatus": "Draft",
      "errorCode": null
    },
    {
      "operation": "activate_policy",
      "status": 200,
      "result": "succeeded",
      "resourceStatus": "Active",
      "errorCode": null
    },
    {
      "operation": "reject_stale_update",
      "status": 412,
      "result": "rejected_as_expected",
      "resourceStatus": "Active",
      "errorCode": "concurrency_conflict"
    },
    {
      "operation": "cancel_policy",
      "status": 200,
      "result": "succeeded",
      "resourceStatus": "Cancelled",
      "errorCode": null
    },
    {
      "operation": "read_audit_trail",
      "status": 200,
      "result": "succeeded",
      "resourceStatus": null,
      "errorCode": null
    }
  ],
  "policy": {
    "status": "Cancelled"
  }
}
```

ETags and identifiers vary on each execution. The actual response also contains the final policy and two complete transition records.

### Rate limit

More than five runs from one client inside one minute returns:

- status: `429 Too Many Requests`;
- stable code: `public_demo_rate_limited`;
- `Retry-After: 60`.

## Verified Azure configuration

All demo resources are isolated in `rg-nain-policy-demo` and colocated in France Central.

| Resource | Verified configuration | Cost boundary |
|---|---|---|
| App Service plan | Linux `F1`, Free | Shared compute, no SLA, 60 CPU minutes/day |
| Web app | .NET 10 LTS, HTTPS only, TLS 1.2, FTPS disabled | Runs only on the F1 plan |
| Azure SQL | General Purpose Serverless, Gen5, 0.5 minimum vCore, 32 GB | Free offer applied |
| Free-limit behavior | `useFreeLimit=true`, `AutoPause` | No continuation into paid overage |
| Backup/storage | Local redundancy, 32 GB maximum, no zone redundancy | Inside the free offer boundary |
| SQL network | 32 exact App Service possible outbound IPs | No `0.0.0.0` allow-all rule |
| Alert | `free_amount_remaining <= 1000`, every 15 minutes | Email warning at approximately 99% consumed |

Azure SQL cannot be forced to stop and remain stopped at exactly 99% consumption. The email alert is preventive. The authoritative no-charge barrier is `AutoPause`, which makes the database unavailable when the monthly free allowance is exhausted and resumes it when the allowance renews.

The action group and alert are enabled. Azure rejected its manual test-notification endpoint with `Free subscription not supported`, so delivery of a test email was not observed. The alert rule itself was read back with the expected metric, threshold, aggregation and schedule.

Cost-budget alerts are useful as a second warning but do not stop resources and can receive delayed cost data. They are not used as the database cutoff.

## Deployment settings

The connection string is stored in App Service configuration and is not present in source control. The active non-secret settings are:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__ApplyMigrationsOnStartup=false
PublicDemo__Enabled=true
PublicDemo__RetentionHours=24
PublicDemo__RequestsPerMinute=5
PolicyOperations__SupportedCurrencies__0=EUR
AllowedHosts=nain-policy-demo-api.azurewebsites.net
```

`Database__ApplyMigrationsOnStartup` was enabled for the first deployment and disabled after all migrations and readiness checks succeeded. A future deployment containing a migration must enable it deliberately for that reviewed release and disable it again after verification.

SQL transient retries are enabled for serverless wake-up. The demo cleanup transaction executes inside the same EF Core retry strategy, so its two deletes and commit remain one retryable atomic unit.

## Run locally

Use a synthetic local database. Do not expose the local port to the internet.

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=.\NAINCONFIGURATOR;Database=PolicyOperationsPublicDemoLocal;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
$env:PublicDemo__Enabled = "true"
$env:PublicDemo__OrganizationId = "33333333-3333-3333-3333-333333333333"
$env:PublicDemo__RetentionHours = "24"
$env:PublicDemo__RequestsPerMinute = "5"
$env:Database__ApplyMigrationsOnStartup = "true"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5055"

dotnet run --project .\GestionFinanciera\PolicyOperations.Api --configuration Release --no-launch-profile
```

Open [http://127.0.0.1:5055/demo/](http://127.0.0.1:5055/demo/). The protected policy endpoints still require a valid JWT.

## Security and operating boundary

- The anonymous endpoint accepts no body or identity data.
- Only the dedicated demo organization is cleaned up.
- The normal policy API remains protected by JWT and roles.
- Swagger contains no token and does not make protected endpoints anonymous.
- `/health` is process liveness; `/health/ready` checks SQL connectivity.
- App Service F1 and Azure SQL Free provide no SLA.
- No customer or personal data is authorized.
- A customer pilot still requires external identity, backup/restore evidence, privacy decisions, monitoring and support.

## Disable and recover

Set `PublicDemo__Enabled=false` and restart the app to remove the anonymous operation and page. To retire the deployment, first confirm that `rg-nain-policy-demo` still contains only synthetic demo resources, export any evidence that must be retained, and then remove the resource group through a reviewed operation. No destructive down migration is required.
