# Spec 005 — Intent-based warm-up and free-quota guard

- Document version: 0.1
- Status: Proposed
- Date: 2026-10-26
- Owner and approval authority: Aitor Nain Mendoza Vallejo
- Governing decisions: ADR-005 (0 EUR, free hosting), ADR-006, ADR-007 (proposed)
- Amends: spec 004 (`REQ-130`, `REQ-134`, `REQ-136`, `REQ-137`), see "Supersession"

## Problem

Verified read-only against Azure on 2026-10-26:

| Fact | Value |
|---|---|
| Database | Free offer, `AutoPause` on exhaustion, auto-pause delay 60 min, minimum 0.5 vCore |
| Subscription | Pay-as-you-go, spending limit off, no budget configured |
| Free allowance | 100,000 vCore-seconds per month |

Spec 004 wakes the database whenever the page loads. Each wake keeps the database online for the
work plus the 60-minute idle delay, an estimated 1,800 vCore-seconds (estimate, not measured), so
about 55 wakes exhaust the month. Crawlers and bots load pages without a human, so they would
spend the allowance. When it is exhausted the database stays paused until the next month and the
public demo can only show `Simulated`.

Separately, a click during a cold start waits up to 6 s, and the replay still takes about 1.4 s
when the goal is about 1 s.

## Goal

Spend free quota only for visitors who show intent, never leave a visitor waiting on a sleeping
database, and finish a run in about 1 s.

## Non-goals

- No paid infrastructure and no keep-alive polling.
- No change to the API, persistence, CORS or domain rules.
- No claim that a cold start can be live in 1 s: that needs an always-on database (paid), a
  separate decision.
- The label stays honest: `Simulated` is never shown as `Live`.

## Requirements

Notation: EARS ("When X, the system shall Y").

### Intent-based warm-up

- `REQ-140` When the client loads, the system shall not contact the API until the visitor shows
  intent.
- `REQ-141` When the first of pointer movement, pointer press, touch, key press or scroll happens
  after load, the system shall start the wake-up once and shall ignore every later signal.
- `REQ-142` When the visitor starts a run without an earlier intent signal, the system shall start
  the wake-up as part of that run.

### Run policy

- `REQ-143` When the visitor runs the scenario while the wake-up is pending, the system shall wait
  for it at most 0.8 s; if it becomes `ready` within that time it shall run live, otherwise it
  shall return the in-browser result labelled `Simulated` without sending the demo request.
- `REQ-144` When the wake-up is `ready`, `unavailable` or was never started, the system shall send
  the demo request and wait for it at most 2 s before returning the in-browser result labelled
  `Simulated`.
- `REQ-145` When a live response arrives inside its limit, the system shall label it `Live`.
- `REQ-146` When the replay starts, the system shall start its fifth frame no later than 0.8 s
  after the first, so the replay including the 0.3 s visual transition ends within 1.1 s.
  `REQ-138` (order, reduced motion) still holds.

### Cost guard (owner actions, not code)

- `REQ-147` The repository shall document the owner steps that lower the database auto-pause delay
  to 15 min and create a monthly budget of 1 EUR with e-mail alerts, and shall state that they
  were not executed by the agent.

## Supersession

| Spec 004 requirement | State |
|---|---|
| `REQ-130` warm on load | Replaced by `REQ-140` to `REQ-142` |
| `REQ-134` wait at most 6 s | Replaced by `REQ-143`, `REQ-144` |
| `REQ-136` run independent of pending wake-up | Replaced by `REQ-143` |
| `REQ-137` replay within 1.5 s | Replaced by `REQ-146` |
| `REQ-131`, `REQ-132`, `REQ-133`, `REQ-135`, `REQ-138` | Unchanged |

## Open questions

- Whether the free offer allows a 15 min auto-pause delay is not verified; the runbook says what to
  do if Azure rejects it.
