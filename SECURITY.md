# Security policy

## Scope

This repository holds a portfolio demonstration of an insurance policy operations API. It is not a
production service and holds no customer or personal data. The public demo at
`https://nain-policy-demo-api.azurewebsites.net/demo/` runs on free-tier infrastructure with
synthetic data only and carries no service-level agreement.

## Reporting a vulnerability

Report suspected vulnerabilities privately to **contact@naindev.com**. Please include:

- what you observed and where;
- the steps to reproduce it;
- the impact you believe it has.

Expect an acknowledgement within five working days. Because this is a personal portfolio project
with no paid support, there is no guaranteed remediation window, and there is no bug bounty.

Please do not open a public issue for a suspected vulnerability, and please do not run automated
scanners, load tests or denial-of-service tests against the demo deployment: it runs on a free
monthly allowance, and exhausting it takes the portfolio link offline for everyone.

## What is in scope

- The source code in this repository.
- The behaviour of the public demo endpoint and page.

## What is out of scope

- Missing rate limiting or availability guarantees beyond the documented free-tier boundary; the
  absence of an SLA is a recorded decision, not a defect.
- Cold starts and `AutoPause` resumes on the free tier.
- Findings that depend on running the API with development settings, `dotnet user-jwts` tokens or
  `Database:ApplyMigrationsOnStartup` enabled. Those are local development affordances and are
  documented as unsuitable for public exposure.
- Third-party infrastructure operated by Microsoft Azure.

## Security posture of this repository

- No secret, connection string or signing key is tracked in source control. Configuration values
  live in user secrets locally and in App Service configuration when deployed.
- All policy endpoints require a validated JWT; the organization and actor come only from token
  claims and can never be selected by a request body.
- Error responses are sanitised Problem Details with stable codes; no SQL text, stack trace or
  connection detail is returned.
- Dependencies are checked on every CI run (`dotnet list package --vulnerable`, which fails the
  build on a finding) and kept current by Dependabot. CodeQL analyses the C# sources on push and
  weekly.

See `docs/business-rules.md` and `docs/architecture.md` for the authoritative security and trust
boundary, and `docs/public-demo.md` for the verified deployment configuration.
