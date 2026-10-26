# Tasks 004 — Prewarm the live demo

- Document version: 0.1
- Date: 2026-10-26
- Labels: `[A]` agent completes, `[M]` mixed, `[H]` owner only

## Status

| ID | Task | Label | Done when | State |
|---|---|---|---|---|
| `T-401` | Write failing tests for `REQ-130` to `REQ-136` | `[A]` | `npm test` fails only on the new cases | Done (9 failed before implementation) |
| `T-402` | Implement `api.warmUp()` and the run timeout | `[A]` | New tests pass | Done (27 of 27 pass) |
| `T-403` | Call the warm-up on mount and show the status chip | `[A]` | Build clean; chip visible in a browser run | Done (dev server against Azure: `Database ready`, `Live · SQL Server`) |
| `T-404` | Update traceability, changelog, public-demo evidence | `[A]` | Docs match the observed numbers | Done except the cold-start number (`T-406`) |
| `T-407` | Shorten the blueprint replay to finish within 1.5 s (`REQ-137`, `REQ-138`) | `[A]` | Tests pass; browser click to done at or under 1.5 s | Done (31 of 31 pass; 1.36 to 1.40 s measured) |
| `T-405` | Approve spec 004 and merge | `[H]` | Owner approves; Pages redeploys | Pending |
| `T-406` | Time the first click after the page has loaded for 30 s on a paused database | `[M]` | Number recorded in `docs/public-demo.md` | Pending |

## Session handoff

- **Done and verified:** spec, plan, tasks, tests, implementation. Client: 27 tests pass, `npm run build` clean.
- **Observed:** live run from the dev client against Azure: click to last frame 1.36 to 1.40 s
  (API about 0.1 to 0.2 s plus a 1.2 s replay at 0.3 s per step).
- **Not verified:** a true cold start after a long idle (`T-406`).
- **Next:** `T-405` approval and merge.
- **Blocked:** `T-405` and `T-406` need the owner.
