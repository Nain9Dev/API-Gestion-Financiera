# Tasks 002 — Live architecture blueprint

- Document version: 0.1
- Date: 2026-10-22
- Labels: `[A]` agent completes, `[M]` mixed, `[H]` owner only

## Status

| ID | Task | Label | Done when | State |
|---|---|---|---|---|
| `T-101` | Record ADR-007 as `Proposed` | `[A]` | File exists in `docs/decisions/` and is listed in `docs/README.md` | Done |
| `T-102` | Add Vitest to the client with a `test` script | `[A]` | `npm test` runs and reports suites | Done |
| `T-103` | Write `blueprintTimeline.test.ts` (red) | `[A]` | Tests fail for `REQ-102`, `REQ-103`, `REQ-104` | Done |
| `T-104` | Implement `blueprintTimeline.ts` (green) | `[A]` | `T-103` passes | Done |
| `T-105` | Write `api.test.ts` for source labelling and base URL (red) | `[A]` | Tests fail for `REQ-110`, `REQ-111` | Done |
| `T-106` | Implement `runScenario`, `telemetry.ts`, `VITE_API_BASE_URL`, `.env.production` (green) | `[A]` | `T-105` passes | Done |
| `T-107` | Write `PreflightFromPolicySubdomainIsAllowed` (red), then add the origin in `Program.cs` (green) | `[A]` | xUnit test passes against SQL Server | Done |
| `T-108` | Replace `style.css` values with brand tokens; update fonts in `index.html` | `[A]` | `npm run build` clean, every view renders on the new tokens | Done |
| `T-109` | Implement `ArchitectureBlueprint.vue` | `[A]` | Renders all nodes, reacts to a frame, respects reduced motion | Done |
| `T-110` | Rewrite `ScenarioView.vue` around the blueprint, compact `HeaderNav.vue` and footer | `[A]` | First screen is the blueprint; run replays steps; source label visible | Done |
| `T-111` | Trim secondary views to title plus one subtitle | `[A]` | No `description`, `explanation-box` or rule paragraphs remain | Done |
| `T-112` | Browser walkthrough: desktop, mobile width, reduced motion, keyboard | `[A]` | Observations recorded in `docs/public-demo.md` | Done (reduced motion not emulated) |
| `T-113` | Verification gate: `dotnet build`, `dotnet format`, `dotnet test` with SQL Server, `npm run build`, `npm test` | `[A]` | All green and reported | Done (one pre-existing failure, see handoff) |
| `T-114` | Update `docs/changelog.md`, `docs/traceability.md`, `docs/README.md`, `docs/architecture.md` | `[A]` | Documents reference spec 002 and ADR-007 | Done |
| `T-115` | Approve or reject ADR-007 | `[H]` | ADR status updated by the owner | Pending |
| `T-116` | Deploy the API with the new CORS origin (part of `T-019`) | `[M]` | Live run from `policy.naindev.com` shows `Live` | Pending |

## Session handoff

- **Done (2026-10-22):** `T-101` to `T-111`. ADR-007 written (`Proposed`). Vitest added; 16 client
  tests pass. `blueprintTimeline.ts`, `telemetry.ts`, `ArchitectureBlueprint.vue`, rewritten
  `ScenarioView.vue` and `HeaderNav.vue`, brand-token `style.css`, `.env.production`, CORS origin
  `https://policy.naindev.com` in `Program.cs` with `PreflightFromPolicySubdomainIsAllowed` green.
  Client contracts aligned to the real API shape (`operation`, `status`, `result`, `policy`,
  `occurredAtUtc`, `actorSubject`, `correlationId`); secondary views trimmed.
- **Verified:** `npm run build` (vue-tsc) clean; `npm test` 16/16; `dotnet build` Release 0
  warnings; `dotnet format` clean; Domain 22/22, Application 25/25, Integration 25/26. Browser
  walkthrough in the Vite dev server at desktop width: blueprint renders, run replays five steps,
  412 drawn red at the guard, ledger ends with two rows, source label shows `Simulated` (no local
  backend). Lifecycle, Concurrency, Audit and Resilience tabs render on the new tokens.
- **Also done:** `T-112` (desktop and 375 px mobile walkthrough recorded in `docs/public-demo.md`;
  the sheet scrolls sideways on phones at 760 px minimum) and `T-114` (changelog 0.9,
  traceability 0.2 with suite `C`, `docs/README.md` registry, `architecture.md` 0.8,
  `public-demo.md` 0.4, README row).
- **Final gate (2026-10-22):** `dotnet build` Release 0 warnings / 0 errors; `dotnet format` clean;
  Domain 22/22, Application 25/25, Integration 25/26; `npm run build` clean; `npm test` 16/16.
- **Pre-existing failure, fixed on this branch:** `SwaggerExplainsQuotedEtagAndDocumentsJsonResponses`
  requested `/swagger/v1/swagger.json`, removed by the Scalar migration in commit `ee39423`, and
  broke CI on `main`. Spec 003 (`specs/003-openapi-contract-test/`) restores the contract evidence
  against `/openapi/v1.json` and is included in the same pull request so CI can go green.
- **Not verified:** `prefers-reduced-motion` was not emulated in a browser (code path only). A
  `Live` run from `policy.naindev.com` needs the API redeployed with the new CORS origin.
  `.claude/launch.json` was added for the browser preview and is untracked.
- **Next:** owner decides `T-115` (ADR-007) and schedules `T-116` with `T-019`.
- **Blocked:** `T-115` and `T-116` need the owner. Nothing committed.
