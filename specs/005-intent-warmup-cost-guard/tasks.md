# Tasks 005 — Intent-based warm-up and free-quota guard

- Document version: 0.1
- Date: 2026-10-26
- Labels: `[A]` agent completes, `[M]` mixed, `[H]` owner only

## Status

| ID | Task | Label | Done when | State |
|---|---|---|---|---|
| `T-501` | Write failing tests for `REQ-140` to `REQ-146` | `[A]` | `npm test` fails only on the new cases | Done (5 failed in `warmUp`/`blueprintTimeline`, `warmIntent` suite missing) |
| `T-502` | Implement `warmIntent`, the run policy and the 0.2 s replay step | `[A]` | All tests pass; build clean | Done (43 of 43 pass) |
| `T-503` | Adapt the spec 004 tests that assumed the demo request was the first call | `[A]` | `api.test.ts` green | Done |
| `T-504` | Browser check against Azure | `[A]` | No request before intent; live run about 1 s warm; simulated at about 1.6 s on a paused database | Done |
| `T-505` | Write the Azure cost-guard runbook | `[A]` | File exists, commands verified against `az --help` | Done |
| `T-506` | Lower the auto-pause delay to 15 min | `[H]` | `az sql db show` returns `autoPauseDelay` 15 | Pending |
| `T-507` | Create the 1 EUR monthly budget with e-mail alerts | `[H]` | Budget visible in Cost Management | Pending |
| `T-508` | Approve spec 005 and merge | `[H]` | Owner approves; Pages redeploys | Pending |

## Session handoff

- **Done and verified:** spec, tests first, implementation, 43 client tests, build, browser runs.
- **Observed (dev client against Azure):**
  - after load with no input: 0 requests to the API;
  - run on a database that had paused: chip `Waking free-tier database`, result `Simulated`, click to
    done 1.63 to 1.65 s; the database then took 43 s to answer `/health/ready`;
  - run on the awake database: `Live · SQL Server`, click to done 1.15 s (includes starting the
    wake-up), then 0.97 to 0.98 s.
- **Not verified:** the pointer signal was delivered in the first pass only (in the second the pane
  was not drawn, so that run started the wake-up itself, `REQ-142`). Touch, key and scroll are
  covered by unit tests only; real-device behaviour is not checked.
- **Next:** `T-506`, `T-507`, `T-508`.
- **Blocked:** the three tasks above need the owner.
