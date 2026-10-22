# Spec 003 — OpenAPI contract evidence after the Scalar migration

- Document version: 0.1
- Status: Proposed
- Date: 2026-10-22
- Owner and approval authority: Aitor Nain Mendoza Vallejo
- Governing decisions: ADR-003 (lifecycle security), ADR-005 (public demo boundary)

## Problem

Commit `ee39423` replaced Swashbuckle with the native `Microsoft.AspNetCore.OpenApi` generator
and the Scalar reference at `/docs`. Two things were lost without being recorded:

1. The operation filter that documented the contract (`If-Match` required, quoted ETag example,
   `application/json` on success and `application/problem+json` on errors, Bearer scheme with a
   global security requirement, anonymous demo endpoint) was deleted and not replaced. The native
   generator emits `If-Match` as optional with no description, lists `application/json`,
   `text/json` and `text/plain` on every response, and publishes no security scheme at all.
2. The document moved from `/swagger/v1/swagger.json` (served by middleware, outside
   authorization) to `/openapi/v1.json` (an endpoint, subject to the authenticated-user fallback
   policy). Anonymous requests now receive `401`, so the Scalar page cannot load its own
   document and `SwaggerExplainsQuotedEtagAndDocumentsJsonResponses` fails on `main`.

`docs/traceability.md` maps the rule *"Quoted `If-Match` example and JSON media types in OpenAPI"*
to that test, so the repository currently claims evidence it does not have.

## Decision: the contract description stays anonymous

The OpenAPI document and the `/docs` reference are served without authentication, as the Swagger
middleware was before the migration. Rationale:

- The document describes the contract; it contains no policy data, no token and no secret.
- `docs/public-demo.md` already states that the API explorer carries no token and does not make
  protected endpoints anonymous. Protected operations keep their Bearer requirement in the
  document and their authorization policy at runtime.
- Scalar fetches the document from the browser without credentials; a protected document makes
  the reference unusable, including on the public demo deployment.

Making the test authenticate instead would hide the broken `/docs` page behind a green test.

## Non-goals

- No change to `/api/v1/policies` contracts, domain rules or persistence schema.
- No change to who may call a protected endpoint.
- No reintroduction of Swashbuckle.

## Requirements

- `REQ-030` When an anonymous client requests `/openapi/v1.json`, the API shall return `200` with
  the OpenAPI document.
- `REQ-031` When an anonymous client requests `/docs/`, the API shall return the Scalar reference
  rather than `401`.
- `REQ-032` When the OpenAPI document describes `POST /api/v1/policies/{policyId}/activate` or
  `POST /api/v1/policies/{policyId}/cancel`, the `If-Match` header parameter shall be marked
  required, carry the example `"AAAAAAAAAAE="` and a description that says the quotation marks
  are part of the value.
- `REQ-033` When the OpenAPI document describes a response of any operation, successful (`2xx`)
  responses shall list `application/json` only and error responses shall list
  `application/problem+json` only.
- `REQ-034` When the OpenAPI document is generated, it shall publish a `Bearer` HTTP security
  scheme (`bearer`, `JWT`) and a global security requirement that references it.
- `REQ-035` When an operation is marked `[AllowAnonymous]`, its OpenAPI description shall carry
  an empty security requirement list so the global Bearer requirement does not apply to it.

## Acceptance

`OpenApiDocumentExplainsQuotedEtagAndDocumentsJsonResponses` in
`PolicyOperations.IntegrationTests` fails when any of `REQ-030`, `REQ-032` to `REQ-035` is removed.
`REQ-031` is verified by manual request against the running API (see `plan.md`).

## Session handoff

- **Done and verified:** `REQ-030`, `REQ-032` to `REQ-035` demonstrated by the renamed test
  against SQL Server. `REQ-031` observed with `curl` on a local Release build (`/docs/` → `200`).
- **Not done:** nothing in this spec. Documentation drift from the migration itself
  (`docs/architecture.md` still names Swashbuckle, `docs/local-demo.md` and `README.md` still
  describe the Swagger UI walkthrough) is outside this spec and listed as a follow-up.
- **Blocked:** nothing.
