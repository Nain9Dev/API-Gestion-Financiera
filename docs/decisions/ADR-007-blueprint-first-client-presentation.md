# ADR-007: Blueprint-first client presentation and honest execution source

- Identifier: ADR-007
- Date: 2026-10-22
- Status: Proposed
- Decision owner: Aitor Nain Mendoza Vallejo

## Context

`PolicyOperations.Client`, published at `https://policy.naindev.com/`, is the first executable
artifact most visitors from the portfolio open. It was built as a generic dark dashboard: every tab
opens with a paragraph that *describes* the architecture, and the architecture itself is never
drawn. The repository banner and the brand manual already define a "technical blueprint" identity
(navy grid, Urbanist, IBM Plex Mono, brand green and red) that the client does not use.

Two correctness problems were found while reviewing the deployed client:

1. It calls `/api/v1/demo/run` relative to its own origin. On GitHub Pages that is a `404`, so it
   silently runs the in-browser engine and presents the result with the same "verified" wording as
   a real run. `AGENTS.md` forbids claims that exceed the evidence.
2. The API CORS policy does not include `https://policy.naindev.com`, so a correct base URL alone
   would not fix it.

The client has no test runner, so none of its behaviour is demonstrated by a test.

## Decision

### Presentation

- The first view of the client is a live architecture blueprint: an SVG map of Client, API,
  Application, Domain (with the three lifecycle states), Infrastructure and SQL Server (with the
  `Policies` table and the append-only `PolicyTransitions` ledger).
- Running the scenario replays the returned steps through that map. A `412` is drawn as a rejected
  path at the concurrency guard; successful state changes move the domain state and append ledger
  rows.
- Text is reduced to labels, HTTP status codes and measured durations. Secondary views keep their
  function with a title and at most one subtitle.
- The client adopts the NainDev brand tokens: Urbanist, IBM Plex Mono, brand blue `#0047AB`, green
  `#80FF00`, red `#FF001F`, 4 px radius and the blueprint grid used by the repository banner.
- Animations respect `prefers-reduced-motion`; the blueprint carries an accessible name and
  description; step summaries are keyboard reachable.

### Honest execution source

- The client labels every run `Live` or `Simulated` according to where the response came from. The
  simulated label never uses the live colour.
- The API base URL is `VITE_API_BASE_URL`, committed in `.env.production` for the Pages build and
  absent in development so the Vite proxy keeps working.
- `https://policy.naindev.com` is added to the API CORS origins. The policy otherwise stays as
  approved in `OPEN-012`: same methods, same headers, same exposed headers.

### Verification

- Vitest is added as a development dependency of the client. The blueprint timeline and the
  source-labelling contract are covered by unit tests. The CORS origin is covered by the API suite.
- No runtime dependency is added to the client.

## Consequences

- The showcase can now be shown to a client without reading; the architecture is the content.
- A visitor on the Pages site reaches the Azure API only after the API is redeployed with the new
  origin (`T-019`). Until then every run is labelled `Simulated`, which is the honest state.
- Calling the Azure host from the Pages site consumes the same free F1 quota the `/demo/` page
  already uses. No new cost is introduced; the 0 EUR boundary of ADR-005 stands.
- The server-rendered Spanish `/demo/` page is unchanged by this decision.

## Rejected alternatives

- Keep the simulation as the only path and relabel it: rejected, because the portfolio link
  promises a live SQL-backed run.
- Add an animation or diagramming library: rejected, the SVG is small enough to own and a
  dependency would need its own review.
- Translate the client to Spanish in the same change: deferred; the client locale is a product
  decision not covered here.
