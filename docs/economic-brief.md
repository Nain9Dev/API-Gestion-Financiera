# Economic brief

- Document version: 0.5
- Status: Draft
- Date: 2026-08-02
- Decision owner: Aitor Nain Mendoza Vallejo
- Currency: EUR

## Current recommendation

`Insufficient evidence` for a sellable product or paid implementation. Continue only with product approval, a bounded technical demo and zero-cash commercial validation.

## Why

There is no verified buyer, problem interview, willingness to pay, pilot, usage or delivery-cost evidence. Technical implementation cannot substitute for that evidence.

The approved product direction and first technical slice reduce delivery uncertainty, but they do not provide commercial evidence. No buyer, pilot or willingness-to-pay signal has been observed through this implementation work.

## Commercial hypothesis

| Item | Hypothesis | Evidence strength |
|---|---|---|
| Customer | Small insurance brokerage or insurtech operations team | E0 — assumption |
| Buyer | Owner, operations manager or technical lead | E0 — assumption |
| Problem | Fragmented policy state and weak auditability create errors and manual review | E0 — assumption |
| Offer | Bounded policy-operations API plus integration/setup service | E0 — assumption |
| Price | TBD after problem and offer validation | No estimate authorized |
| Channel | Portfolio, professional network and direct outreach | E1 — channels exist; conversion unknown |
| Differentiator | Inspectable domain rules, SQL Server behavior and executable tests | E1 — portfolio positioning only |

Evidence scale used here:

- `E0`: unsupported assumption;
- `E1`: indirect or internal signal;
- `E2`: direct problem conversation;
- `E3`: concrete follow-up commitment;
- `E4`: bounded pilot or signed commercial commitment;
- `E5`: collected revenue and observed delivery outcome.

## Offer boundary

Proposed validation offer:

- a local or recorded technical demo;
- a workflow review using synthetic examples;
- a bounded pilot proposal only after security, legal, operations and budget review.

Excluded from a standard offer:

- custom code forks;
- real underwriting advice;
- unlimited integrations or support;
- guaranteed availability before infrastructure is implemented and measured;
- processing real personal or sensitive data during validation.

## Current resource guardrails

| Resource | Limit | State |
|---|---:|---|
| Incremental cash before commercial evidence | 0 EUR | Confirmed owner constraint |
| Paid services | None | Confirmed by task |
| Credit cards or consumption billing | Not allowed without explicit approval | Required |
| Maximum additional owner hours before commercial evidence | 40 hours from 2026-08-02 | Approved guardrail |

The 40-hour guardrail covers the remaining technical demo, preparation and commercial validation work. It does not include unattended automated execution, but it does include the owner's active engineering, review, outreach, meeting and support time. Stop expanding the product when the cap is reached unless stronger commercial evidence or a new explicit budget is recorded.

## Zero-cash delivery plan

| Need | Zero-cash option | License/limit note | Exit trigger |
|---|---|---|---|
| Runtime and SDK | .NET 10 LTS | Free and open source; Microsoft support is scheduled through 2028-11-14 | Reassess before end of support or if a security requirement demands it |
| IDE | Visual Studio Community for individual use or existing editor | Individual may create free or paid apps; organization rules differ | Team or organization usage exceeds license conditions |
| Local database | Installed SQL Server 2025 Standard Developer | Free only for development and test; it is not licensed for production | Any customer pilot that stores operational data requires an approved production database and recovery plan |
| Source and CI | Public GitHub repository and standard GitHub-hosted runners | Public standard-runner Actions usage is free; storage/metered extras still need controls | Private code, extra storage or paid runner need |
| API exploration | OpenAPI UI and tracked `.http` requests | Swashbuckle is MIT; compatibility must be reviewed on upgrade | Approved consumer requires another format/tool |
| Demo exposure | One-click synthetic façade on Azure App Service F1 and Azure SQL Free | No SLA; stop SQL at free limit; not deployed without an active subscription | Free limits harm the demo or qualified buyer needs a pilot |

“Zero cash” excludes the economic cost of owner time and existing hardware, electricity, internet and domain. Those costs must be tracked before pricing.

Official references checked on 2026-08-01:

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [SQL Server 2025 editions and supported features](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025)

## Proposed validation experiment

| Field | Proposed value |
|---|---|
| Decision | Is this problem strong enough to justify completing the technical demo and testing a paid pilot? |
| Sample | 10 qualified contacts in insurance operations or technical roles |
| Cash cap | 0 EUR |
| Owner-hours cap | 40 additional hours across demo completion and validation |
| Data | Interview notes without unnecessary personal data; synthetic demo only |
| Primary signal | Independently reported problem with concrete current impact |
| Commitment signal | Second workflow review or bounded pilot discussion |
| Deadline | Before the 40-hour guardrail is exhausted |

Proposed decision thresholds:

- `Continue`: at least 4 problem confirmations, 3 follow-up commitments and 1 bounded pilot discussion.
- `Change`: repeated problem exists but the user, buyer, workflow or offer differs materially; record the new hypothesis before more code.
- `Pause`: access to the segment is temporarily unavailable; define a restart date and stop implementation spend.
- `Stop`: fewer than 2 credible problem confirmations after the completed sample, or safe delivery cannot fit the approved constraints.

The owner-hour guardrail is approved. The contact thresholds remain decision rules for the experiment, not probabilities or market estimates.

## Metrics

| Metric | Definition | Decision use |
|---|---|---|
| `ProblemConfirmationRate` | Qualified contacts independently confirming the problem / qualified contacts interviewed | Problem gate |
| `FollowUpCommitmentRate` | Contacts accepting a dated second step / qualified contacts interviewed | Offer signal |
| `PilotDiscussionCount` | Named organizations discussing scope, responsibility and price | Commercial commitment signal |
| `OwnerHours` | All project, outreach, demo and support hours | Investment guardrail |
| `IncrementalCashOutflow` | Cash paid specifically for this project | Zero-cash guardrail |

GitHub stars, page views and generic compliments are secondary signals, not validation of willingness to pay.

## Pricing and unit economics

No price, revenue, margin, CAC, LTV or payback figure is defensible yet. Before a pilot, measure:

- setup and integration hours;
- infrastructure and backup cost;
- recurring support hours;
- sales and onboarding time;
- external service and payment cost;
- collected cash, not only proposed or invoiced revenue.

Separate setup, recurring service and professional services when their cost drivers differ. Do not include unlimited customer-specific development in a standard subscription.

## Commercial and operational blockers

- product and buyer hypotheses unvalidated;
- price and delivery responsibility undefined;
- repository license is MIT; third-party dependency licenses still require review;
- customer authentication, privacy, backup, restoration and support not implemented;
- real-data processing not authorized;
- owner-hour tracking has not started;
- Azure subscription is inactive, so the public backend and live portfolio button do not exist yet.

## Next economic decision

Track the approved 40-hour guardrail and stop product expansion if it is exhausted without direct problem or commitment evidence. The authorized incremental cash commitment remains 0 EUR.

## Change history

| Version | Date | Change | Approval |
|---:|---|---|---|
| 0.5 | 2026-08-02 | Added the approved zero-cash public-demo target and recorded the inactive Azure subscription blocker | Hosting target approved; commercial hypotheses remain Draft |
| 0.4 | 2026-08-02 | Edited wording for clarity; no commercial threshold changed | Commercial hypotheses remain Draft |
| 0.3 | 2026-08-02 | Approved 40-hour guardrail and recorded MIT repository license | Guardrail and license approved; commercial hypotheses remain Draft |
| 0.2 | 2026-08-01 | Aligned runtime, local database limits and technical evidence with the implemented slice | Not approved |
| 0.1 | 2026-08-01 | Initial minimum economic brief | Not approved |
