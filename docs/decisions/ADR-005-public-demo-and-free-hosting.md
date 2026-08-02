# ADR-005: Public portfolio demo and free hosting

- Identifier: ADR-005
- Date: 2026-08-02
- Status: Approved
- Decision owner: Aitor Nain Mendoza Vallejo

## Context

The local Swagger walkthrough proves the lifecycle, but it asks a reviewer to install tools, configure SQL Server and create a development token. The portfolio needs a shorter path that remains safe on the public internet and does not create a recurring cash commitment.

ADR-003 keeps the policy API protected by JWT and forbids exposing local development tokens. A public demo must not weaken that boundary or accept personal data from anonymous visitors.

## Decision

### Public interaction

- Add one anonymous operation, `POST /api/v1/demo/run`, disabled by default.
- The operation accepts no request body and uses a fixed server-side demo organization and actor.
- Each run creates only synthetic values and executes the existing application services: create Draft, activate, reject a stale version, cancel and read the audit trail.
- The normal `/api/v1/policies` endpoints keep their JWT, role and organization requirements unchanged.
- A same-origin page at `/demo/` presents the result without registration, downloads or a token.

### Abuse and data controls

- Apply a per-client fixed-window rate limit configured between 1 and 30 runs per minute.
- Store public-demo rows under one dedicated organization identifier.
- Delete expired rows for that organization before each run; the initial retention limit is 24 hours.
- Accept no names, documents, contact details or free-text supplied by visitors.
- Return no-store responses and apply a restrictive security policy to the demo page.

### Hosting

- The approved public-demo target is Azure App Service Free F1 plus the Azure SQL Database free offer.
- Use the default HTTPS App Service hostname for the first publication.
- Configure Azure SQL to stop when its monthly free allowance is exhausted rather than continue as paid usage.
- Do not enable paid plans, consumption fallback or billable auxiliary resources without a new explicit approval.
- The free deployment is portfolio infrastructure only: it has no SLA, no availability promise and may have cold starts or quota stops.

## Consequences and trade-offs

- A reviewer can observe the main business flow with one button and no account.
- The demo proves the same Domain, Application and SQL behavior, but the anonymous façade is not a customer identity system.
- The fixed input prevents anonymous personal-data collection and keeps the public attack surface small.
- The free tier is suitable for low portfolio traffic, not a customer pilot or production service.
- Opportunistic cleanup means expired rows remain until a later run if the demo receives no traffic.

## Rejected alternatives

- **Publish a shared JWT:** rejected because a long-lived public credential would be copied and abused.
- **Remove authentication from policy endpoints:** rejected because it would replace an approved security contract.
- **Require a social login:** rejected for the portfolio demo because it adds friction without improving the synthetic fixed scenario.
- **Host from the owner's computer:** rejected because a personal workstation is not public infrastructure.
- **CloudAMQP:** rejected because a message broker does not host ASP.NET Core or SQL Server and the current monolith does not need one.
- **Free HTTP-only ASP.NET hosting:** rejected because authentication and API traffic require HTTPS.

## Affected components and documents

- `PolicyOperations.Api`: public-demo controller, page, rate limiting, retention and readiness.
- `PolicyOperations.Infrastructure`: existing SQL persistence used without schema changes.
- `docs/public-demo.md`, architecture, business rules, roadmap and public README.
- The portfolio project card after a live URL is observed.

## Migration and recovery impact

No schema migration is required. Deployment applies the existing migration history to an empty Azure SQL database. Disabling `PublicDemo:Enabled` removes public-demo access without changing the protected API. All public-demo data is synthetic and may be deleted with the dedicated resource group after verification.

## Implementation evidence

Implemented on 2026-08-02 in France Central:

- Linux App Service plan read back as `F1` / `Free`;
- web app read back as .NET 10, HTTPS-only, TLS 1.2 and FTPS disabled;
- Azure SQL read back as General Purpose Serverless with `useFreeLimit=true`, 32 GB maximum, local backup redundancy and `AutoPause` at exhaustion;
- SQL firewall restricted to the App Service possible outbound IP set;
- one metric alert enabled at 1,000 remaining vCore-seconds, evaluated every 15 minutes;
- public HTTPS smoke returned healthy liveness/readiness and the complete five-step lifecycle with two audit transitions.

The 99% alert is an early warning, not a cutoff. Azure SQL Free stops at 100% exhaustion through `AutoPause`, which is the control that prevents paid overage. The free subscription rejected a manual action-group test notification, so email delivery remains unobserved until a real alert fires.

## Reconsideration triggers

Reconsider the façade when a qualified buyer needs individual identity, persistent workspaces, personal input, guaranteed availability, more than 30 runs per minute or a customer pilot.

## Approval record

Approved by Aitor Nain Mendoza Vallejo on 2026-08-02 through explicit authorization to prepare and deploy a safe free public demo linked from the portfolio.
