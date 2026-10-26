# Plan 004 — Prewarm the live demo

## Design

All changes are in `PolicyOperations.Client`; the API is untouched.

1. `services/api.ts`
   - `api.warmUp()` returns a module-level singleton promise (`REQ-131`) that resolves to
     `'ready' | 'unavailable'`. It does a real `GET {BASE_URL}/health/ready` with `cache: 'no-store'`
     and an `AbortSignal.timeout(120_000)`. It never falls back to `mockPolicyEngine` (`REQ-132`).
   - `api.runScenario()` passes `AbortSignal.timeout(6_000)` to the demo `fetch` (`REQ-134`).
     The existing `catch` already routes an abort to the simulated engine, so the label stays
     truthful (`REQ-135`).
   - Warm-up state (`pending | ready | unavailable`) is exposed through a small subscribe function
     so the view can render it without a store dependency.
2. `App.vue` calls `api.warmUp()` on mount (`REQ-130`).
3. `views/ScenarioView.vue` shows the status chip in the control strip (`REQ-133`). The run button
   is never disabled by warm-up (`REQ-136`).

## Why a 6 s run limit

A warm run takes about 0.3 to 1.2 s. 6 s leaves room for a slow first request on the F1 plan and
still gives the visitor an answer instead of a spinner that can last 100 s.

## Cost

One extra `GET /health/ready` per page load. It resumes the database, which the first run would
have done anyway; no polling and no timers, so no new steady-state consumption.

## Tests

Vitest only, in `services/api.test.ts` and a new `services/warmUp.test.ts`, with `fetch` mocked and
fake timers. No new dependency.
