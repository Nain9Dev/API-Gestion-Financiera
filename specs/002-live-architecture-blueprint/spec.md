# Spec 002 — Live architecture blueprint

- Document version: 0.1
- Status: Proposed
- Date: 2026-10-22
- Owner and approval authority: Aitor Nain Mendoza Vallejo
- Governing decisions: ADR-005 (public demo boundary), ADR-006 (free-tier resilience),
  ADR-007 (blueprint-first client presentation, proposed)

## Problem

`https://policy.naindev.com/` is the showcase a prospective client opens from the portfolio. Today it
is a generic dark "glass" dashboard: five tabs, each opening with a paragraph of prose, badges and a
button. The architecture the repository exists to demonstrate (Clean Architecture layers, the
lifecycle state machine, optimistic concurrency, the append-only ledger) is never *shown*; it is
described in text the visitor has to read.

Two further observations against the deployed client:

| Observation | Impact |
|---|---|
| The client calls `/api/v1/demo/run` relative to its own origin. On GitHub Pages that is a `404`, so every run silently falls back to the in-browser engine | A visitor sees "Run Verified Scenario" and gets a simulation presented as live evidence. This contradicts the "no claim without evidence" boundary |
| The API CORS policy allows `naindev.com` and `www.naindev.com` but not `policy.naindev.com` | Even with a correct base URL the browser would block the call |

## Goal

Make the architecture the product. The first screen is a live blueprint of the system; running the
scenario animates a real request through it. Text is reduced to labels, status codes and
measurements.

## Non-goals

- No change to `/api/v1/policies` contracts, domain rules, persistence or the Azure deployment.
- No change to the Spanish server-rendered `/demo/` page hosted with the API.
- No new runtime dependency in the client; only a test runner as a development dependency.
- No paid infrastructure, no analytics, no third-party scripts beyond the existing ecosystem bar.

## Requirements

Notation: EARS ("When X, the system shall Y").

### Blueprint surface

- `REQ-101` When the client loads, the system shall present the architecture blueprint as the first
  view: an SVG map with the nodes Client, API, Application, Domain (with the three lifecycle
  states), Infrastructure and SQL Server (with `Policies` and the append-only `PolicyTransitions`
  ledger), connected in dependency order.
- `REQ-102` When the visitor runs the scenario and a result is available, the blueprint shall replay
  the returned steps in order, each step lighting the layers it crosses and moving the domain state
  to the step's `resultingStatus`.
- `REQ-103` When a replayed step has expected status `412`, the blueprint shall render it as a
  rejected path at the concurrency guard, shall not change the domain state and shall not append a
  ledger row.
- `REQ-104` When a replayed step succeeds with a state change, the ledger shall gain exactly one
  row, so a complete run ends with two rows and the final policy in `Cancelled`.
- `REQ-105` When a run completes, each step shall be summarised by at most one line: index, method,
  path, HTTP status and measured duration in milliseconds.

### Honest execution source

- `REQ-110` When the scenario response comes from the configured API, the page shall label the run
  `Live`; when it comes from the in-browser fallback, the page shall label the run `Simulated`.
  The two labels shall be visually distinct and the simulated label shall never use the live colour.
- `REQ-111` When the client is built for production, the API base URL shall come from
  `VITE_API_BASE_URL`; when the variable is absent, the client shall keep using same-origin paths
  so the local Vite proxy keeps working.
- `REQ-112` When a browser on `https://policy.naindev.com` calls the API, the API shall accept the
  origin under the existing CORS policy, exposing `ETag` and `Retry-After` as today.
- `REQ-113` When the client types the demo response, the field names shall match the API contract
  (`operation`, `status`, `result`, `resourceStatus`, `dataRetentionHours`, `policy`), so a live
  response renders without undefined values. The in-browser fallback shall produce the same shape.

### Visual system

- `REQ-120` When any view renders, it shall use the NainDev brand tokens: Urbanist for headings,
  IBM Plex Mono for technical values, brand blue, green and red for state, 4 px radius and the
  blueprint grid background already used by the repository banner.
- `REQ-121` When a secondary view (lifecycle workbench, concurrency lab, audit trail, resilience
  probe) renders, it shall open with a title and at most one short subtitle; explanatory paragraphs
  are removed.
- `REQ-122` When the visitor prefers reduced motion, the blueprint shall show the final state of
  each step without travelling animations.
- `REQ-123` When the blueprint renders, it shall carry an accessible name and description, the step
  summaries shall be reachable by keyboard, and text contrast shall meet WCAG 2.2 AA.

## Acceptance evidence

| Requirement | Evidence |
|---|---|
| `REQ-102` `REQ-103` `REQ-104` | Vitest: `blueprintTimeline.test.ts` builds frames from the five canonical steps and asserts lit nodes, domain state, ledger count and tone per frame |
| `REQ-110` `REQ-111` | Vitest: `api.test.ts` stubs `fetch` and asserts `source: 'live'` on `200`, `source: 'simulated'` on network failure, and the base URL prefix |
| `REQ-112` | xUnit: `PreflightFromPolicySubdomainIsAllowed` in `PoliciesApiTests` |
| `REQ-101` `REQ-105` `REQ-120` `REQ-121` `REQ-123` | Browser walkthrough recorded in `docs/public-demo.md` with the date; `vue-tsc` build clean |
| `REQ-122` | Browser walkthrough with `prefers-reduced-motion: reduce` emulated |
