# Plan 002 — Live architecture blueprint

- Document version: 0.1
- Status: Proposed
- Date: 2026-10-22
- Implements: `spec.md` in this directory

## Design decisions

### 1. The blueprint is data first, SVG second

`src/architecture/blueprintTimeline.ts` is a pure module with no Vue import. It declares the node
and edge catalogue of the system and exposes `buildTimeline(steps)`: for each
`PublicDemoStepResponse` it returns a `BlueprintFrame` with the lit nodes, the travelled edges, the
domain state after the step, the ledger row count and a `tone` (`ok` or `rejected`).

Rationale: the animation logic becomes testable without a DOM, and the SVG component only paints
whatever frame it receives. A future view (the concurrency lab, the audit trail) can reuse the same
frames.

### 2. One SVG component, CSS-driven states

`src/components/ArchitectureBlueprint.vue` renders a fixed `viewBox` and reads one `frame` prop.
Nodes and edges toggle classes (`lit`, `rejected`, `visited`); packets travel with SMIL
`animateMotion` started from script so the component can skip them under
`prefers-reduced-motion`. No animation library is added.

### 3. The scenario view becomes the blueprint view

`ScenarioView.vue` keeps its tab id (`scenario`) so nothing else moves, but its content is now:
blueprint, a one-line control strip (run button, elapsed time, source label, run id) and a strip of
five step chips. The raw JSON stays available behind a `details` element.

While the request is in flight the Client→API edge pulses and the elapsed timer runs, because a
cold free-tier run can take tens of seconds and a silent wait reads as a failure (ADR-006).

### 4. Honest source

`api.runScenario()` returns `{ source: 'live' | 'simulated', response }`. The existing
`api.runPublicDemo()` keeps its signature for the other views and delegates to it. Telemetry for
the ecosystem drawer is emitted from one `telemetry.ts` helper for both sources.

`BASE_URL` reads `import.meta.env.VITE_API_BASE_URL ?? ''`. A committed `.env.production` points
the Pages build at the Azure host. `Program.cs` adds `https://policy.naindev.com` to the CORS
origins; nothing else in the policy changes.

### 5. Brand tokens replace the generic glass theme

`style.css` keeps the variable names the views already consume (`--bg-surface`, `--accent-*`,
`--font-*`, `--radius-*`, `.glass-panel`, `.badge-*`, `.btn-*`) and changes their values to the
brand: Urbanist and IBM Plex Mono, blueprint navy `#060D1A` with the grid, brand green `#80FF00`
as the success and primary colour, brand red `#FF001F` for rejection, `#38BDF8` for flow. This
restyles every view without touching their scoped CSS.

The secondary views lose their intro paragraphs and explanatory boxes; headings and controls stay.

### 6. Tests

Vitest is added as a development dependency with a `test` script. Two suites cover the pure module
and the API source contract. The CORS origin is covered by the existing API test class against the
real SQL Server suite, because that is where request-level tests live in this repository.

## Out of scope and risks

- The Azure deployment still runs the pre-ADR-006 build (`T-019` pending). Until it is deployed, a
  cold live run can still fail; the client then shows `Simulated`, which is the honest outcome.
- Calling the Azure host from the Pages site consumes the free F1 quota as the `/demo/` page
  already does. No new cost is introduced.
