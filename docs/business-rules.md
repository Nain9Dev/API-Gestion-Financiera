# Business rules and API contract

- Document version: 0.5
- Status: Active
- Date: 2026-08-02
- Governing decisions: ADR-001, ADR-002 and ADR-003

## Status meanings

- `Approved implemented`: authoritative and verified in code/tests.
- `Approved pending`: authoritative direction, not yet implemented.
- `TBD`: no implementation authority; do not infer.
- `Historical`: behavior from the replaced pre-1.0 prototype.

## Policy identity and value

| ID | Rule | Enforcement | State |
|---|---|---|---|
| `POL-001` | Server generates an immutable non-empty `PolicyId` | Domain | Approved implemented |
| `POL-002` | `PolicyNumber` is required, trimmed and limited to 50 characters | Domain and SQL | Approved implemented |
| `POL-003` | Normalized number is `ToUpperInvariant()` | Domain | Approved implemented |
| `POL-004` | Normalized number is unique inside one `OrganizationId` | Pre-check and SQL composite unique index | Approved implemented |
| `POL-005` | Different organizations may use the same normalized number | Organization-scoped queries and index | Approved implemented |
| `POL-006` | `InsuredAmount` is greater than zero and at most `9,999,999,999,999,999.99` | Domain, SQL positive check and `decimal(18,2)` | Approved implemented |
| `POL-007` | Amount has at most two decimal places and is never silently rounded | Domain and SQL precision | Approved implemented |
| `POL-008` | Currency is required and normalized to an uppercase three-letter ISO 4217 shape | Domain and SQL check | Approved implemented |
| `POL-009` | Currency must also be enabled by deployment configuration | Application allow-list | Approved implemented |
| `POL-010` | Local supported currencies are `EUR`, `GBP` and `USD` | Versioned configuration | Approved implemented |
| `POL-011` | Amount and currency are immutable in the current slice | No mutation command | Approved implemented |
| `POL-012` | Creation time uses UTC offset zero | Domain and `datetimeoffset(7)` | Approved implemented |

The ISO alphabetic structure is stable domain syntax. The configurable allow-list is the commercial/operational decision of one deployment and can change without a schema migration.

## Organization and authorization

| ID | Rule | Enforcement | State |
|---|---|---|---|
| `SEC-001` | All policy endpoints require JWT Bearer authentication | Fallback authorization policy | Approved implemented |
| `SEC-002` | `PolicyReader` can read policies and transitions | Authorization policy | Approved implemented |
| `SEC-003` | `PolicyOperator` can read, create, activate and cancel | Authorization policy | Approved implemented |
| `SEC-004` | Actor comes from immutable JWT `sub`/name-identifier | API trusted context | Approved implemented |
| `SEC-005` | Organization comes from a valid non-empty GUID `organization_id` claim | Authorization and API trusted context | Approved implemented |
| `SEC-006` | Request bodies cannot select actor or organization | Public contracts exclude both fields | Approved implemented |
| `SEC-007` | Reads, counts, uniqueness and audit queries always include organization scope | Repository | Approved implemented |
| `SEC-008` | A resource in another organization is returned as `404` | Scoped lookup | Approved implemented |
| `SEC-009` | Public responses do not expose SQL, stack traces, connection strings or internal exceptions | API error boundary | Approved implemented |
| `SEC-010` | Local demo uses synthetic data and `dotnet user-jwts` only | Demo boundary | Approved |

The local issuer is not approved for public use. A customer environment requires an approved external OpenID Connect issuer and operational identity procedures.

## Lifecycle and completeness

| From | Command | To | Required command data | State |
|---|---|---|---|---|
| None | `CreatePolicy` | `Draft` | policy number, insured amount and currency | Approved implemented |
| `Draft` | `ActivatePolicy` | `Active` | insured-party reference, coverage start/end and current ETag | Approved implemented |
| `Draft` | `CancelPolicy` | `Cancelled` | reason and current ETag | Approved implemented |
| `Active` | `CancelPolicy` | `Cancelled` | reason and current ETag | Approved implemented |
| `Active` | `ActivatePolicy` | None | conflict | Approved implemented |
| `Cancelled` | Any lifecycle command | None | conflict | Approved implemented |

Activation rules:

- `InsuredPartyReference` is an opaque upstream reference, required, trimmed and at most 100 characters.
- It must not contain names, identity documents, addresses or other real personal data in the demo.
- `CoverageStartDate` and `CoverageEndDate` are calendar dates.
- End date may equal but cannot precede start date.
- SQL independently prevents an `Active` row with incomplete activation data.

Cancellation rules:

- reason is required, trimmed and at most 500 characters;
- the reason is included in the transition record;
- cancellation is terminal and no deletion endpoint exists.

## Concurrency and retry behavior

| ID | Rule | State |
|---|---|---|
| `CONC-001` | SQL Server `rowversion` protects the complete policy row | Approved implemented |
| `CONC-002` | Create/get/activate/cancel responses expose a strong ETag | Approved implemented |
| `CONC-003` | Mutation requires exactly one quoted strong base64 `If-Match` value | Approved implemented |
| `CONC-004` | Missing `If-Match` returns `428 precondition_required` | Approved implemented |
| `CONC-005` | Invalid/weak/multiple ETag returns `400 etag_invalid` | Approved implemented |
| `CONC-006` | Stale ETag returns `412 concurrency_conflict` without a persisted transition | Approved implemented |
| `CONC-007` | Invalid transition using the current ETag returns `409 policy_transition_invalid` | Approved implemented |

Lifecycle commands are intentionally not silently idempotent. A caller must reload after a timeout or conflict and decide from the observed current status. Automatic state-changing retries without a fresh ETag are forbidden.

The `version` JSON property contains only base64 data. The `ETag` response header wraps that value in quotation marks, and callers must copy the complete header value into `If-Match`. For example, version `AAAAAAAAAAE=` is sent as `If-Match: "AAAAAAAAAAE="`.

## Audit trail

Every accepted activation or cancellation appends exactly one record containing:

- transition identifier;
- policy and organization identifiers;
- previous and new status;
- UTC timestamp;
- authenticated actor subject;
- correlation identifier;
- cancellation reason when applicable.

The policy update and audit insert use one EF Core `SaveChanges` transaction. No API update or delete contract exists for transitions. Results are ordered by occurrence time and identifier.

## Pagination

| ID | Rule | State |
|---|---|---|
| `PAGE-001` | Page number begins at 1 | Approved implemented |
| `PAGE-002` | Page size is between 1 and 100 | Approved implemented |
| `PAGE-003` | Skip calculation cannot overflow a 32-bit SQL offset | Approved implemented |
| `PAGE-004` | Order is policy number then identifier inside the organization | Approved implemented |

## Public portfolio demo

| ID | Rule | State |
|---|---|---|
| `DEMO-001` | The public demo is disabled unless deployment configuration explicitly enables it | Approved implemented |
| `DEMO-002` | `POST /api/v1/demo/run` rejects a request body and accepts no visitor-selected policy data | Approved implemented |
| `DEMO-003` | Organization and actor are fixed server-side and dedicated to synthetic portfolio runs | Approved implemented |
| `DEMO-004` | One run uses the existing application services for Draft, activation, stale-version rejection, cancellation and audit | Approved implemented |
| `DEMO-005` | Protected policy endpoints retain JWT, role and organization checks | Approved implemented |
| `DEMO-006` | A fixed-window per-client limit allows 1 to 30 configured runs per minute | Approved implemented |
| `DEMO-007` | Before a run, only expired policies and transitions in the configured demo organization are deleted | Approved implemented |
| `DEMO-008` | The initial public-demo retention is 24 hours and the page uses no-store responses | Approved implemented |

The façade is portfolio evidence, not an identity provider or customer API contract. A visitor cannot enter a name, document, contact detail, organization, amount, reason or arbitrary free text.

## Implemented API

| Method | Route | Success | Relevant errors |
|---|---|---|---|
| `POST` | `/api/v1/policies` | `201` | `400`, `401`, `403`, `409` |
| `GET` | `/api/v1/policies/{policyId}` | `200` | `401`, `403`, `404` |
| `GET` | `/api/v1/policies` | `200` | `400`, `401`, `403` |
| `POST` | `/api/v1/policies/{policyId}/activate` | `200` | `400`, `401`, `403`, `404`, `409`, `412`, `428` |
| `POST` | `/api/v1/policies/{policyId}/cancel` | `200` | `400`, `401`, `403`, `404`, `409`, `412`, `428` |
| `GET` | `/api/v1/policies/{policyId}/transitions` | `200` | `401`, `403`, `404` |
| `GET` | `/health` | `200` | Process liveness only |
| `GET` | `/health/ready` | `200` | SQL Server connectivity; `503` when unavailable |
| `POST` | `/api/v1/demo/run` | `200` | Anonymous only when enabled; `404`, `429`, `500` |

Stable error codes include:

- `authentication_required`, `forbidden`;
- `policy_not_found`, `policy_number_conflict`, `policy_transition_invalid`;
- `precondition_required`, `etag_invalid`, `concurrency_conflict`;
- `policy_number_required`, `policy_number_too_long`;
- `insured_amount_invalid`, `insured_amount_precision_invalid`;
- `currency_required`, `currency_invalid`, `currency_not_supported`;
- `insured_party_reference_required`, `insured_party_reference_too_long`;
- `coverage_period_invalid`;
- `cancellation_reason_required`, `cancellation_reason_too_long`;
- `validation_failed`, `internal_error`.

## Postman-ready lifecycle example

All values are synthetic. Replace identifiers, token and ETags with values observed from the preceding response.

### Create Draft policy

- Method: `POST`
- URL: `https://localhost:7024/api/v1/policies`
- Required headers: `Authorization: Bearer <LOCAL_TOKEN>`, `Content-Type: application/json`, `Accept: application/json`

```json
{
  "policyNumber": "SYNTH-2026-0001",
  "insuredAmount": 125000.00,
  "currency": "EUR"
}
```

Expected status: `201 Created`.

Representative expected response header:

```text
ETag: "AAAAAAAAAAE="
```

Representative expected response:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "organizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "policyNumber": "SYNTH-2026-0001",
  "insuredAmount": 125000.00,
  "currency": "EUR",
  "insuredPartyReference": null,
  "coverageStartDate": null,
  "coverageEndDate": null,
  "status": "Draft",
  "createdAtUtc": "2026-08-02T12:00:00+00:00",
  "version": "AAAAAAAAAAE="
}
```

### Activate policy

- Method: `POST`
- URL: `https://localhost:7024/api/v1/policies/11111111-1111-1111-1111-111111111111/activate`
- Required headers: `Authorization: Bearer <LOCAL_TOKEN>`, `If-Match: "AAAAAAAAAAE="`, `Content-Type: application/json`

```json
{
  "insuredPartyReference": "SYNTH-INSURED-0001",
  "coverageStartDate": "2026-09-01",
  "coverageEndDate": "2027-08-31"
}
```

Expected status: `200 OK`; status becomes `Active` and the response returns a new ETag.

### Cancel policy

- Method: `POST`
- URL: `https://localhost:7024/api/v1/policies/11111111-1111-1111-1111-111111111111/cancel`
- Required headers: `Authorization: Bearer <LOCAL_TOKEN>`, `If-Match: <LATEST_QUOTED_ETAG>`, `Content-Type: application/json`

```json
{
  "reason": "Synthetic customer request"
}
```

Expected status: `200 OK`; status becomes `Cancelled`.

### Read audit trail

- Method: `GET`
- URL: `https://localhost:7024/api/v1/policies/11111111-1111-1111-1111-111111111111/transitions`
- Required headers: `Authorization: Bearer <LOCAL_TOKEN>`, `Accept: application/json`

Representative expected response:

```json
[
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "policyId": "11111111-1111-1111-1111-111111111111",
    "fromStatus": "Draft",
    "toStatus": "Active",
    "occurredAtUtc": "2026-08-02T12:01:00+00:00",
    "actorSubject": "demo-operator",
    "reason": null,
    "correlationId": "representative-trace-1"
  },
  {
    "id": "33333333-3333-3333-3333-333333333333",
    "policyId": "11111111-1111-1111-1111-111111111111",
    "fromStatus": "Active",
    "toStatus": "Cancelled",
    "occurredAtUtc": "2026-08-02T12:02:00+00:00",
    "actorSubject": "demo-operator",
    "reason": "Synthetic customer request",
    "correlationId": "representative-trace-2"
  }
]
```

### Stale ETag error

Repeat a mutation with an older ETag.

Expected status: `412 Precondition Failed`.

Representative expected response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.13",
  "title": "Policy version conflict.",
  "status": 412,
  "detail": "The policy changed after the supplied version was read. Reload it and retry.",
  "instance": "/api/v1/policies/11111111-1111-1111-1111-111111111111/cancel",
  "code": "concurrency_conflict",
  "traceId": "representative-trace-id"
}
```

## Migration boundary

The old boolean `IsActive` and local `IssueDate` behavior remains superseded. The lifecycle migration preserves its timestamp value with UTC offset zero because the legacy schema had no timezone.

The organization/security migration applies automatically only when `Policies` is empty. SQL error `51003` stops an upgrade containing rows because organization, currency and activation data cannot be inferred safely. Down migration error `51004` prevents removal of organization, currency, audit and concurrency data while any policy exists.

## Risk-assessment boundary

Risk assessment remains unapproved for implementation. Inputs, factors, scale, versioning and non-advisory wording are still `TBD`; the current demo must not be described as underwriting, pricing or eligibility advice.
