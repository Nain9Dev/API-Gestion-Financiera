# Local visual demo

- Document version: 0.2
- Status: Active
- Date: 2026-08-02
- Environment: Local development only

## What this demonstrates

Swagger UI provides a visual client for the real API. The scenario proves:

1. JWT authentication and role authorization;
2. organization-scoped policy access;
3. `Draft -> Active -> Cancelled` lifecycle rules;
4. optimistic concurrency through `ETag` and `If-Match`;
5. append-only transition history with actor and correlation data;
6. SQL Server persistence using synthetic data.

It does not prove production identity, hosting, backup, privacy compliance or customer readiness.

## 1. Configure and migrate the local database

Run from the repository root in PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=.\NAINCONFIGURATOR;Database=PolicyOperationsLocalDemo;Integrated Security=True;TrustServerCertificate=True;Encrypt=False"
$env:POLICY_OPERATIONS_MIGRATIONS_SQLSERVER = $env:ConnectionStrings__DefaultConnection

dotnet tool restore
dotnet restore .\GestionFinanciera\GestionFinanciera.sln
dotnet tool run dotnet-ef database update --project .\GestionFinanciera\PolicyOperations.Infrastructure --startup-project .\GestionFinanciera\PolicyOperations.Infrastructure --configuration Release
```

The database already exists on the verified development computer as of 2026-08-02. Re-running `database update` is safe when the recorded migration history is unchanged. Always verify the server and database name before executing it.

## 2. Create an eight-hour local JWT

```powershell
dotnet user-jwts create --project .\GestionFinanciera\PolicyOperations.Api\PolicyOperations.Api.csproj --name demo-operator --role PolicyOperator --role PolicyReader --claim organization_id=11111111-1111-1111-1111-111111111111 --valid-for 8h
```

Copy only the generated token value. The signing key is stored through .NET user-secrets in the local user profile, not in the repository. This issuer is for local development only.

## 3. Start the API

Use the same PowerShell session so the connection string remains available:

```powershell
dotnet run --project .\GestionFinanciera\PolicyOperations.Api --configuration Release --launch-profile https
```

Open [https://localhost:7024/swagger](https://localhost:7024/swagger). If the HTTPS development certificate is not trusted, use [http://localhost:5047/swagger](http://localhost:5047/swagger).

Select **Authorize**, paste the token without adding quotation marks, and confirm. Swagger adds the `Bearer` prefix.

The token and an ETag are entered differently: the token has no quotation marks, while an ETag keeps the quotation marks returned by the API.

## 4. Create a Draft policy

Open `POST /api/v1/policies`, select **Try it out**, and send:

```json
{
  "policyNumber": "SYNTH-DEMO-0001",
  "insuredAmount": 125000.00,
  "currency": "EUR"
}
```

Expected status: `201 Created`.

Copy:

- `id` from the response body;
- the quoted `ETag` value from response headers.

The response must show `status: "Draft"` and the trusted demo `organizationId`.

## 5. Activate the policy

Open `POST /api/v1/policies/{policyId}/activate`.

- Enter the policy identifier.
- Paste the complete quoted ETag into `If-Match`, for example `"AAAAAAAAAAE="`.
- Send:

```json
{
  "insuredPartyReference": "SYNTH-INSURED-0001",
  "coverageStartDate": "2026-09-01",
  "coverageEndDate": "2027-08-31"
}
```

Expected status: `200 OK` and `status: "Active"`.

Copy the new ETag. It must differ from the Draft ETag.

If Swagger sends `AAAAAAAAAAE=` without the surrounding quotation marks, the API returns `400 etag_invalid`. That is expected: copy the response header exactly instead of copying the unquoted `version` property from the JSON body.

## 6. Demonstrate stale-update protection

Call the cancellation endpoint with the old Draft ETag. Expected result:

- HTTP `412 Precondition Failed`;
- stable code `concurrency_conflict`;
- policy remains `Active`.

This is the visible proof that a stale client cannot overwrite a newer accepted transition.

## 7. Cancel with the current ETag

Open `POST /api/v1/policies/{policyId}/cancel`, use the newest ETag and send:

```json
{
  "reason": "Synthetic customer request"
}
```

Expected status: `200 OK` and `status: "Cancelled"`.

## 8. View the audit trail

Open `GET /api/v1/policies/{policyId}/transitions`.

The response must contain two ordered records:

1. `Draft -> Active`;
2. `Active -> Cancelled` with the cancellation reason.

Each record contains the JWT actor subject, UTC timestamp and request correlation identifier.

## 9. Optional organization-isolation proof

Create another token with a different organization:

```powershell
dotnet user-jwts create --project .\GestionFinanciera\PolicyOperations.Api\PolicyOperations.Api.csproj --name other-demo-operator --role PolicyOperator --role PolicyReader --claim organization_id=22222222-2222-2222-2222-222222222222 --valid-for 8h
```

After authorizing Swagger with the second token:

- the first organization's policy identifier returns `404`;
- the list does not contain the first organization's policies;
- the same policy number can be created independently.

## Safety boundary

- Use only identifiers beginning with `SYNTH-` and no real personal data.
- Do not expose ports `5047` or `7024` to the internet.
- SQL Server Standard Developer is not licensed for production.
- Local JWTs are not a customer authentication system.
- Stop the API with `Ctrl+C` when the demonstration finishes.

The tracked request collection in [PolicyOperations.Api.http](../GestionFinanciera/PolicyOperations.Api/PolicyOperations.Api.http) provides the same flow without Swagger.
