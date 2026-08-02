# Public portfolio demo

- Document version: 0.1
- Status: Implemented and verified locally; Azure deployment blocked
- Date: 2026-08-02
- Governing decision: ADR-005

## Current result

The one-click page is available locally at `http://127.0.0.1:5055/demo/` when the public-demo configuration is enabled. A browser run against SQL Server 2025 observed:

1. `201` — Draft created;
2. `200` — policy activated;
3. `412 concurrency_conflict` — stale version rejected;
4. `200` — current version cancelled;
5. `200` — two audit transitions returned.

The Azure account inspected on 2026-08-02 has no active subscription. The personal account offers a new trial and the existing Azure for Students subscription is disabled. No trial, payment method or cloud resource was created.

## Public contract

### Run the fixed scenario

- Method: `POST`
- URL: `https://<APP_NAME>.azurewebsites.net/api/v1/demo/run`
- Required headers: `Accept: application/json`
- Request body: none
- Authentication: none
- Expected status after deployment: `200 OK`

A request body is rejected with `400 Bad Request` and stable code `public_demo_body_not_allowed`.

Representative expected response:

```json
{
  "runId": "A1B2C3D4",
  "executedAtUtc": "2026-08-02T12:00:00+00:00",
  "dataRetentionHours": 24,
  "steps": [
    {
      "operation": "create_draft",
      "status": 201,
      "result": "succeeded",
      "resourceStatus": "Draft",
      "etag": "\"AAAAAAAAAAE=\"",
      "errorCode": null
    },
    {
      "operation": "reject_stale_update",
      "status": 412,
      "result": "rejected_as_expected",
      "resourceStatus": "Active",
      "etag": "\"AAAAAAAAAAE=\"",
      "errorCode": "concurrency_conflict"
    }
  ],
  "policy": {
    "status": "Cancelled"
  },
  "transitions": []
}
```

The real response includes five steps, the complete final policy and two transitions. The shortened example above is not evidence of a public deployment.

### Rate limit

More than the configured number of runs from one client inside one minute returns:

- Expected status: `429 Too Many Requests`
- Stable code: `public_demo_rate_limited`
- `Retry-After: 60`

## Run locally

Use a synthetic local database. Do not expose the port to the internet.

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

## Azure deployment checklist

Do not start this checklist until the owner has an active subscription and confirms that Azure is not requesting a paid conversion.

1. Create one dedicated resource group.
2. Create Azure SQL Database using the free offer and select the option to pause when the monthly free allowance is exhausted.
3. Create an App Service plan using the exact `F1` Free SKU and one code-only .NET 10 web app.
4. Use the default `https://<APP_NAME>.azurewebsites.net` hostname.
5. Keep the SQL connection string in App Service configuration, never in GitHub or `appsettings.json`.
6. Configure the values below.
7. Apply the existing three migrations to the empty database.
8. Verify `/health`, `/health/ready`, `/demo/`, `/swagger` and one complete demo run.
9. Confirm the Azure cost view remains at 0 EUR before adding the portfolio link.

Required App Service configuration:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__ApplyMigrationsOnStartup=true
PublicDemo__Enabled=true
PublicDemo__OrganizationId=<DEDICATED_DEMO_GUID>
PublicDemo__RetentionHours=24
PublicDemo__RequestsPerMinute=5
PolicyOperations__SupportedCurrencies__0=EUR
AllowedHosts=<APP_NAME>.azurewebsites.net
```

After the first successful migration, `Database__ApplyMigrationsOnStartup` may be set to `false` until a reviewed deployment includes another migration.

## Security and operating boundary

- The anonymous endpoint accepts no body or identity data.
- Only the dedicated demo organization is cleaned up.
- The normal policy API remains protected by JWT and roles.
- Swagger contains no token and does not make protected endpoints anonymous.
- `/health` is process liveness; `/health/ready` checks SQL connectivity.
- App Service F1 and Azure SQL Free provide no SLA.
- A customer pilot still requires external identity, backup/restore evidence, privacy decisions, monitoring and support.

## Disable and recover

Set `PublicDemo__Enabled=false` and restart the app to remove the anonymous operation and page. If the deployment must be removed, first confirm that the resource group contains only this synthetic demo, then delete the resource group through a separately reviewed operation. No destructive rollback migration is required.
