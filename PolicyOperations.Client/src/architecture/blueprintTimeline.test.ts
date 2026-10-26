import { describe, expect, it } from 'vitest';
import type { PublicDemoStepResponse } from '../types/contracts';
import { buildTimeline, IDLE_FRAME, replayDelayMs, VISUAL_TRANSITION_MS } from './blueprintTimeline';

const policyPath = '/api/v1/policies/9f1c7a52-0000-4000-8000-000000000001';

function step(overrides: Partial<PublicDemoStepResponse>): PublicDemoStepResponse {
  return {
    operation: 'create_draft',
    method: 'POST',
    path: '/api/v1/policies',
    status: 201,
    result: 'succeeded',
    resourceStatus: 'Draft',
    etag: '"AAAAAAAAAAE="',
    errorCode: null,
    durationMs: 10,
    ...overrides,
  };
}

const canonicalRun: PublicDemoStepResponse[] = [
  step({}),
  step({ operation: 'activate_policy', path: `${policyPath}/activate`, status: 200, resourceStatus: 'Active', etag: '"AAAAAAAAAAI="', durationMs: 18 }),
  step({ operation: 'reject_stale_update', path: `${policyPath}/cancel`, status: 412, result: 'rejected_as_expected', resourceStatus: 'Active', etag: '"AAAAAAAAAAE="', errorCode: 'concurrency_conflict', durationMs: 11 }),
  step({ operation: 'cancel_policy', path: `${policyPath}/cancel`, status: 200, resourceStatus: 'Cancelled', etag: '"AAAAAAAAAAM="', durationMs: 16 }),
  step({ operation: 'read_audit_trail', method: 'GET', path: `${policyPath}/transitions`, status: 200, resourceStatus: null, etag: null, durationMs: 8 }),
];

describe('buildTimeline', () => {
  it('starts from an idle frame with no lit nodes, no state and an empty ledger', () => {
    expect(IDLE_FRAME.litNodes).toEqual([]);
    expect(IDLE_FRAME.litEdges).toEqual([]);
    expect(IDLE_FRAME.domainState).toBe('None');
    expect(IDLE_FRAME.ledgerRows).toBe(0);
    expect(IDLE_FRAME.tone).toBe('idle');
  });

  it('returns one frame per step, numbered from one, carrying method, path, status and duration', () => {
    const frames = buildTimeline(canonicalRun);

    expect(frames).toHaveLength(5);
    expect(frames.map((f) => f.index)).toEqual([1, 2, 3, 4, 5]);
    expect(frames[0]).toMatchObject({ method: 'POST', path: '/api/v1/policies', status: 201, durationMs: 10 });
    expect(frames[4]).toMatchObject({ method: 'GET', status: 200, durationMs: 8 });
  });

  it('lights every layer down to SQL Server for a successful lifecycle command (REQ-102)', () => {
    const [create] = buildTimeline(canonicalRun);

    expect(create.tone).toBe('ok');
    expect(create.litNodes).toEqual(['client', 'api', 'application', 'domain', 'infrastructure', 'sql']);
    expect(create.litEdges).toEqual([
      'client-api',
      'api-application',
      'application-domain',
      'application-infrastructure',
      'infrastructure-sql',
    ]);
    expect(create.guardHit).toBe(false);
  });

  it('moves the domain state to the resulting status of each successful command (REQ-102)', () => {
    const frames = buildTimeline(canonicalRun);

    expect(frames.map((f) => f.domainState)).toEqual(['Draft', 'Active', 'Active', 'Cancelled', 'Cancelled']);
  });

  it('draws a 412 as a rejected path stopped at the concurrency guard, without touching state or ledger (REQ-103)', () => {
    const frames = buildTimeline(canonicalRun);
    const rejected = frames[2];

    expect(rejected.tone).toBe('rejected');
    expect(rejected.guardHit).toBe(true);
    expect(rejected.litNodes).toEqual(['client', 'api', 'application']);
    expect(rejected.litEdges).toEqual(['client-api', 'api-application']);
    expect(rejected.domainState).toBe(frames[1].domainState);
    expect(rejected.ledgerRows).toBe(frames[1].ledgerRows);
  });

  it('appends one ledger row per state transition so a full run ends with two rows (REQ-104)', () => {
    const frames = buildTimeline(canonicalRun);

    expect(frames.map((f) => f.ledgerRows)).toEqual([0, 1, 1, 2, 2]);
  });

  it('routes a read through Application and Infrastructure without entering the Domain', () => {
    const frames = buildTimeline(canonicalRun);
    const read = frames[4];

    expect(read.tone).toBe('ok');
    expect(read.litNodes).toEqual(['client', 'api', 'application', 'infrastructure', 'sql']);
    expect(read.litEdges).toEqual(['client-api', 'api-application', 'application-infrastructure', 'infrastructure-sql']);
  });

  it('gives each frame a short label derived from the operation', () => {
    const frames = buildTimeline(canonicalRun);

    expect(frames.map((f) => f.label)).toEqual([
      'Create draft',
      'Activate',
      'Stale ETag',
      'Cancel',
      'Audit trail',
    ]);
  });

  it('treats any non-succeeded result as rejected even when the status is unexpected', () => {
    const [frame] = buildTimeline([step({ status: 409, result: 'rejected_as_expected', errorCode: 'state_conflict' })]);

    expect(frame.tone).toBe('rejected');
    expect(frame.domainState).toBe('None');
    expect(frame.ledgerRows).toBe(0);
  });
});

describe('replayDelayMs (spec 004)', () => {
  const frames = [0, 1, 2, 3, 4];

  it('finishes the five-frame replay within 1.5 s including the visual transition (REQ-137)', () => {
    const lastStart = replayDelayMs(frames.length - 1, false);

    expect(lastStart).toBeLessThanOrEqual(1200);
    expect(lastStart + VISUAL_TRANSITION_MS).toBeLessThanOrEqual(1500);
  });

  it('starts the first frame immediately (REQ-137)', () => {
    expect(replayDelayMs(0, false)).toBe(0);
  });

  it('keeps frames in order, each strictly after the previous (REQ-138)', () => {
    for (const reduced of [false, true]) {
      const delays = frames.map((i) => replayDelayMs(i, reduced));
      for (let i = 1; i < delays.length; i++) expect(delays[i]).toBeGreaterThan(delays[i - 1]);
    }
  });

  it('never starts a frame later under reduced motion than with motion allowed (REQ-138)', () => {
    for (const i of frames) {
      expect(replayDelayMs(i, true)).toBeLessThanOrEqual(replayDelayMs(i, false));
    }
  });
});
