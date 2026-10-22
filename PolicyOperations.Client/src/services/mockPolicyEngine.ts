import type {
  ActivatePolicyRequest,
  CancelPolicyRequest,
  CreatePolicyRequest,
  HealthCheckResult,
  PolicyResponse,
  PolicyTransitionResponse,
  PublicDemoRunResponse,
  PublicDemoStepResponse,
} from '../types/contracts';
import { ApiError } from './api';

/**
 * In-browser fallback that produces the same wire shape as PolicyOperations.Api. It exists so the
 * showcase keeps working when the free-tier backend is asleep or unreachable; every run that comes
 * from here is labelled `simulated` by the API service.
 */

const SIMULATED_ORGANIZATION_ID = '33333333-3333-3333-3333-333333333333';

let policySeq = 1;
const policies = new Map<string, { policy: PolicyResponse; versionNum: number }>();
const transitions = new Map<string, PolicyTransitionResponse[]>();

function encodeRowVersion(num: number): string {
  const buf = new Uint8Array(8);
  const view = new DataView(buf.buffer);
  view.setBigUint64(0, BigInt(num));
  let binary = '';
  for (let i = 0; i < buf.length; i++) {
    binary += String.fromCharCode(buf[i]);
  }
  return btoa(binary);
}

function correlationId(sequence: number): string {
  return `0HN${Math.random().toString(36).substring(2, 9).toUpperCase()}:${String(sequence).padStart(8, '0')}`;
}

function isoDate(date: Date): string {
  return date.toISOString().split('T')[0];
}

export const mockPolicyEngine = {
  async runPublicDemo(): Promise<PublicDemoRunResponse> {
    const runId = crypto.randomUUID().substring(0, 8).toUpperCase();
    const policyId = crypto.randomUUID();
    const policyNumber = `SYNTH-POL-2026-${Math.floor(1000 + Math.random() * 9000)}`;
    const now = new Date();

    const etag1 = `"${encodeRowVersion(1)}"`;
    const etag2 = `"${encodeRowVersion(2)}"`;
    const etag3 = `"${encodeRowVersion(3)}"`;

    const steps: PublicDemoStepResponse[] = [
      {
        operation: 'create_draft',
        method: 'POST',
        path: '/api/v1/policies',
        status: 201,
        result: 'succeeded',
        resourceStatus: 'Draft',
        etag: etag1,
        errorCode: null,
        durationMs: 14,
      },
      {
        operation: 'activate_policy',
        method: 'POST',
        path: `/api/v1/policies/${policyId}/activate`,
        status: 200,
        result: 'succeeded',
        resourceStatus: 'Active',
        etag: etag2,
        errorCode: null,
        durationMs: 18,
      },
      {
        operation: 'reject_stale_update',
        method: 'POST',
        path: `/api/v1/policies/${policyId}/cancel`,
        status: 412,
        result: 'rejected_as_expected',
        resourceStatus: 'Active',
        etag: etag1,
        errorCode: 'concurrency_conflict',
        durationMs: 11,
      },
      {
        operation: 'cancel_policy',
        method: 'POST',
        path: `/api/v1/policies/${policyId}/cancel`,
        status: 200,
        result: 'succeeded',
        resourceStatus: 'Cancelled',
        etag: etag3,
        errorCode: null,
        durationMs: 16,
      },
      {
        operation: 'read_audit_trail',
        method: 'GET',
        path: `/api/v1/policies/${policyId}/transitions`,
        status: 200,
        result: 'succeeded',
        resourceStatus: null,
        etag: null,
        errorCode: null,
        durationMs: 8,
      },
    ];

    const policy: PolicyResponse = {
      id: policyId,
      organizationId: SIMULATED_ORGANIZATION_ID,
      policyNumber,
      insuredAmount: 250000.0,
      currency: 'EUR',
      insuredPartyReference: 'CLI-84920412-ES',
      coverageStartDate: isoDate(new Date(now.getTime() - 86400000 * 30)),
      coverageEndDate: isoDate(new Date(now.getTime() + 86400000 * 335)),
      status: 'Cancelled',
      createdAtUtc: new Date(now.getTime() - 100).toISOString(),
      version: encodeRowVersion(3),
    };

    const transList: PolicyTransitionResponse[] = [
      {
        id: crypto.randomUUID(),
        policyId,
        fromStatus: 'Draft',
        toStatus: 'Active',
        occurredAtUtc: new Date(now.getTime() - 50).toISOString(),
        actorSubject: 'public-demo',
        reason: 'Public demo activation',
        correlationId: correlationId(1),
      },
      {
        id: crypto.randomUUID(),
        policyId,
        fromStatus: 'Active',
        toStatus: 'Cancelled',
        occurredAtUtc: now.toISOString(),
        actorSubject: 'public-demo',
        reason: 'Public demo cancellation',
        correlationId: correlationId(2),
      },
    ];

    policies.set(policyId, { policy, versionNum: 3 });
    transitions.set(policyId, transList);

    const totalDurationMs = steps.reduce((acc, s) => acc + s.durationMs, 0);

    return {
      runId,
      executedAtUtc: now.toISOString(),
      dataRetentionHours: 24,
      totalDurationMs,
      cleanupCompleted: true,
      steps,
      policy,
      transitions: transList,
    };
  },

  probeHealth(endpoint: '/health' | '/health/ready'): HealthCheckResult {
    return {
      status: 'Healthy',
      statusCode: 200,
      totalDurationMs: 4,
      timestamp: new Date().toISOString(),
      details: {
        database: 'Simulated',
        engine: 'In-browser fallback',
        endpoint,
        status: 'Healthy',
      },
    };
  },

  getPolicy(policyId: string): { policy: PolicyResponse; etag: string | null } {
    const item = policies.get(policyId);
    if (!item) {
      const synthPolicy: PolicyResponse = {
        id: policyId,
        organizationId: SIMULATED_ORGANIZATION_ID,
        policyNumber: 'POL-2026-SYNTH',
        insuredAmount: 150000,
        currency: 'EUR',
        insuredPartyReference: 'REF-78901234',
        coverageStartDate: '2026-01-01',
        coverageEndDate: '2026-12-31',
        status: 'Active',
        createdAtUtc: new Date().toISOString(),
        version: encodeRowVersion(1),
      };
      policies.set(policyId, { policy: synthPolicy, versionNum: 1 });
      return { policy: synthPolicy, etag: `"${synthPolicy.version}"` };
    }
    return { policy: item.policy, etag: `"${item.policy.version}"` };
  },

  listTransitions(policyId: string): PolicyTransitionResponse[] {
    return transitions.get(policyId) || [];
  },

  createPolicy(req: CreatePolicyRequest): { policy: PolicyResponse; etag: string | null } {
    const id = crypto.randomUUID();
    const versionNum = 1;
    const version = encodeRowVersion(versionNum);
    const now = new Date().toISOString();

    const policy: PolicyResponse = {
      id,
      organizationId: SIMULATED_ORGANIZATION_ID,
      policyNumber: req.policyNumber || `POL-2026-${String(policySeq++).padStart(4, '0')}`,
      insuredAmount: Number(req.insuredAmount) || 50000,
      currency: req.currency || 'EUR',
      insuredPartyReference: null,
      coverageStartDate: null,
      coverageEndDate: null,
      status: 'Draft',
      createdAtUtc: now,
      version,
    };

    policies.set(id, { policy, versionNum });
    transitions.set(id, []);
    return { policy, etag: `"${version}"` };
  },

  activatePolicy(
    policyId: string,
    req: ActivatePolicyRequest,
    ifMatch: string
  ): { policy: PolicyResponse; etag: string | null } {
    const item = policies.get(policyId);
    if (!item) {
      throw new ApiError(404, { status: 404, title: 'Not Found', detail: `Policy ${policyId} not found.` });
    }

    const cleanMatch = ifMatch.replace(/"/g, '').trim();
    if (cleanMatch !== item.policy.version) {
      throw new ApiError(412, {
        status: 412,
        title: 'Precondition Failed',
        detail: 'The requested policy version does not match the current stored state.',
        code: 'concurrency_conflict',
      });
    }

    if (item.policy.status !== 'Draft') {
      throw new ApiError(409, {
        status: 409,
        title: 'Conflict',
        detail: `Cannot activate policy in '${item.policy.status}' state. Only 'Draft' policies can be activated.`,
        code: 'invalid_state_transition',
      });
    }

    const nextVer = item.versionNum + 1;
    const nextVerStr = encodeRowVersion(nextVer);
    const now = new Date().toISOString();

    item.policy.status = 'Active';
    item.policy.insuredPartyReference = req.insuredPartyReference;
    item.policy.coverageStartDate = req.coverageStartDate;
    item.policy.coverageEndDate = req.coverageEndDate;
    item.policy.version = nextVerStr;
    item.versionNum = nextVer;

    const trans = transitions.get(policyId) || [];
    trans.push({
      id: crypto.randomUUID(),
      policyId,
      fromStatus: 'Draft',
      toStatus: 'Active',
      occurredAtUtc: now,
      actorSubject: 'authenticated-operator',
      reason: 'Standard underwriting activation.',
      correlationId: correlationId(trans.length + 1),
    });
    transitions.set(policyId, trans);

    return { policy: item.policy, etag: `"${nextVerStr}"` };
  },

  cancelPolicy(
    policyId: string,
    req: CancelPolicyRequest,
    ifMatch: string
  ): { policy: PolicyResponse; etag: string | null } {
    const item = policies.get(policyId);
    if (!item) {
      throw new ApiError(404, { status: 404, title: 'Not Found', detail: `Policy ${policyId} not found.` });
    }

    const cleanMatch = ifMatch.replace(/"/g, '').trim();
    if (cleanMatch !== item.policy.version) {
      throw new ApiError(412, {
        status: 412,
        title: 'Precondition Failed',
        detail: 'The requested policy version does not match the current stored state.',
        code: 'concurrency_conflict',
      });
    }

    if (item.policy.status === 'Cancelled') {
      throw new ApiError(409, {
        status: 409,
        title: 'Conflict',
        detail: 'Policy is already cancelled.',
        code: 'invalid_state_transition',
      });
    }

    const nextVer = item.versionNum + 1;
    const nextVerStr = encodeRowVersion(nextVer);
    const now = new Date().toISOString();

    const fromStatus = item.policy.status;
    item.policy.status = 'Cancelled';
    item.policy.version = nextVerStr;
    item.versionNum = nextVer;

    const trans = transitions.get(policyId) || [];
    trans.push({
      id: crypto.randomUUID(),
      policyId,
      fromStatus,
      toStatus: 'Cancelled',
      occurredAtUtc: now,
      actorSubject: 'authenticated-operator',
      reason: req.reason || null,
      correlationId: correlationId(trans.length + 1),
    });
    transitions.set(policyId, trans);

    return { policy: item.policy, etag: `"${nextVerStr}"` };
  },
};
