# Spec 004 — Prewarm the live demo

- Document version: 0.1
- Status: Proposed
- Date: 2026-10-26
- Owner and approval authority: Aitor Nain Mendoza Vallejo
- Governing decisions: ADR-005 (free hosting, 0 EUR), ADR-006 (free-tier resilience),
  ADR-007 (blueprint-first client, proposed)

## Problem

Azure SQL free offer pauses the database after idle time (`AutoPause`). Resuming takes 60 to 100 s
(measured 2026-10-26: `/health/ready` 99 s right after a deploy). A visitor who clicks
**Run scenario** on a paused database stares at a spinner until the request ends, because the
client `fetch` has no timeout and the database is only woken by the first run.

Warm runs are fast: 0.28 to 1.2 s wall, 3 to 16 ms per step on the server.

## Goal

By the time a visitor clicks **Run scenario**, the database has had as long as possible to wake
up, and a click never leaves the visitor waiting on a stuck request.

## Non-goals

- No paid infrastructure and no keep-alive polling: the free offer includes 100,000 vCore-seconds
  per month, and holding the database awake around the clock would exceed it.
- No change to the API, domain rules, persistence, CORS or the Azure deployment.
- No change to the Spanish `/demo/` page hosted with the API.
- The source label stays honest: a `Simulated` result is never presented as `Live`.

## Requirements

Notation: EARS ("When X, the system shall Y").

- `REQ-130` When the client loads, the system shall send one `GET /health/ready` to the configured
  API without waiting for a visitor action.
- `REQ-131` While the client stays loaded, the system shall send that wake-up request at most
  once, however many times the page re-renders or the visitor changes tabs.
- `REQ-132` When the wake-up request answers `200`, the system shall report the database as
  `ready`; when it fails, answers with an error status or exceeds 120 s, the system shall report
  it as `unavailable` and shall not substitute a simulated health result.
- `REQ-133` While the wake-up is pending, the system shall show a short status near the run button
  saying the free-tier database is waking; when it ends, the status shall say `ready` or be hidden
  if `unavailable`.
- `REQ-134` When the visitor runs the scenario, the system shall wait for the live API at most
  6 s; if no response arrives in that time, it shall abort the request, produce the in-browser
  result and label it `Simulated`.
- `REQ-135` When a live run answers within that time, the system shall label it `Live` exactly as
  today (`REQ-110`).
- `REQ-136` When the visitor runs the scenario while the wake-up is still pending, the run shall
  not depend on it: it follows `REQ-134` directly.

### Replay duration

The server answers a warm run in 0.3 to 1.2 s, but the blueprint replays the five steps at 1.15 s
each, so the visitor watches about 6 s after the result is already known.

- `REQ-137` When a run result is available and motion is allowed, the blueprint shall start the
  last of the five frames no later than 1.2 s after the first, so the whole replay, including the
  0.3 s visual transition of the last frame, finishes within 1.5 s.
- `REQ-138` While replaying, the blueprint shall keep the frames in order, each starting strictly
  after the previous one; when the visitor prefers reduced motion, no frame shall start later than
  it would with motion allowed.

## Open questions

- None blocking. The 6 s limit and the 120 s wake-up ceiling are tuning values, not contracts.
