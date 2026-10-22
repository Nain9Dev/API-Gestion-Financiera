import type { PublicDemoStepResponse } from '../types/contracts';

/**
 * Pure model behind the architecture blueprint. No DOM, no Vue: a list of API steps becomes a
 * list of frames that say which layers light up, where the domain state is and how many rows the
 * audit ledger holds after each step. The SVG component only paints a frame.
 */

export type NodeId = 'client' | 'api' | 'application' | 'domain' | 'infrastructure' | 'sql';

export type EdgeId =
  | 'client-api'
  | 'api-application'
  | 'application-domain'
  | 'application-infrastructure'
  | 'infrastructure-sql';

export type DomainState = 'None' | 'Draft' | 'Active' | 'Cancelled';

export type FrameTone = 'idle' | 'ok' | 'rejected';

export interface BlueprintFrame {
  /** 1-based position of the step in the run; 0 for the idle frame. */
  index: number;
  operation: string;
  label: string;
  method: string;
  path: string;
  status: number;
  durationMs: number;
  etag: string | null;
  errorCode: string | null;
  litNodes: NodeId[];
  litEdges: EdgeId[];
  /** True when the request was stopped at the concurrency guard in Application. */
  guardHit: boolean;
  domainState: DomainState;
  ledgerRows: number;
  tone: FrameTone;
}

export const IDLE_FRAME: BlueprintFrame = Object.freeze({
  index: 0,
  operation: 'idle',
  label: 'Idle',
  method: '',
  path: '',
  status: 0,
  durationMs: 0,
  etag: null,
  errorCode: null,
  litNodes: [],
  litEdges: [],
  guardHit: false,
  domainState: 'None',
  ledgerRows: 0,
  tone: 'idle',
}) as BlueprintFrame;

const COMMAND_NODES: NodeId[] = ['client', 'api', 'application', 'domain', 'infrastructure', 'sql'];
const COMMAND_EDGES: EdgeId[] = [
  'client-api',
  'api-application',
  'application-domain',
  'application-infrastructure',
  'infrastructure-sql',
];

const READ_NODES: NodeId[] = ['client', 'api', 'application', 'infrastructure', 'sql'];
const READ_EDGES: EdgeId[] = ['client-api', 'api-application', 'application-infrastructure', 'infrastructure-sql'];

const GUARD_NODES: NodeId[] = ['client', 'api', 'application'];
const GUARD_EDGES: EdgeId[] = ['client-api', 'api-application'];

const LABELS: Record<string, string> = {
  create_draft: 'Create draft',
  activate_policy: 'Activate',
  reject_stale_update: 'Stale ETag',
  cancel_policy: 'Cancel',
  read_audit_trail: 'Audit trail',
};

const DOMAIN_STATES: ReadonlyArray<DomainState> = ['Draft', 'Active', 'Cancelled'];

function toDomainState(value: string | null): DomainState | null {
  return value !== null && (DOMAIN_STATES as ReadonlyArray<string>).includes(value)
    ? (value as DomainState)
    : null;
}

function labelFor(operation: string): string {
  const known = LABELS[operation];
  if (known) return known;
  const words = operation.replace(/[_-]+/g, ' ').trim();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

export function buildTimeline(steps: ReadonlyArray<PublicDemoStepResponse>): BlueprintFrame[] {
  const frames: BlueprintFrame[] = [];
  let domainState: DomainState = IDLE_FRAME.domainState;
  let ledgerRows = IDLE_FRAME.ledgerRows;

  steps.forEach((step, position) => {
    const succeeded = step.result === 'succeeded' && step.status < 400;
    const isRead = step.method.toUpperCase() === 'GET';

    let litNodes: NodeId[];
    let litEdges: EdgeId[];
    let guardHit = false;
    let tone: FrameTone;

    if (!succeeded) {
      litNodes = GUARD_NODES;
      litEdges = GUARD_EDGES;
      guardHit = true;
      tone = 'rejected';
    } else if (isRead) {
      litNodes = READ_NODES;
      litEdges = READ_EDGES;
      tone = 'ok';
    } else {
      litNodes = COMMAND_NODES;
      litEdges = COMMAND_EDGES;
      tone = 'ok';

      const next = toDomainState(step.resourceStatus);
      if (next !== null && next !== domainState) {
        if (domainState !== 'None') {
          ledgerRows += 1;
        }
        domainState = next;
      }
    }

    frames.push({
      index: position + 1,
      operation: step.operation,
      label: labelFor(step.operation),
      method: step.method.toUpperCase(),
      path: step.path,
      status: step.status,
      durationMs: step.durationMs,
      etag: step.etag,
      errorCode: step.errorCode,
      litNodes: [...litNodes],
      litEdges: [...litEdges],
      guardHit,
      domainState,
      ledgerRows,
      tone,
    });
  });

  return frames;
}
