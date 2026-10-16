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

function emitTelemetry(type: string, name: string, latencyMs: number, status: number, payload: unknown) {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('nain:telemetry', {
      detail: { type, name, latencyMs, status, payload }
    }));
  }
}

export const mockPolicyEngine = {
  async runPublicDemo(): Promise<PublicDemoRunResponse> {
    const runId = crypto.randomUUID();
    const policyId = crypto.randomUUID();
    const policyNumber = `SYNTH-POL-2026-${Math.floor(1000 + Math.random() * 9000)}`;
    const now = new Date();

    const etag1 = `"${encodeRowVersion(1)}"`;
    const etag2 = `"${encodeRowVersion(2)}"`;
    const etag3 = `"${encodeRowVersion(3)}"`;

    const steps: PublicDemoStepResponse[] = [
      {
        stepKey: 'create_draft',
        method: 'POST',
        path: '/api/v1/policies',
        expectedStatus: 201,
        outcome: 'Success',
        resultingStatus: 'Draft',
        etag: etag1,
        errorCode: null,
        durationMs: 14,
      },
      {
        stepKey: 'activate_policy',
        method: 'POST',
        path: `/api/v1/policies/${policyId}/activate`,
        expectedStatus: 200,
        outcome: 'Success',
        resultingStatus: 'Active',
        etag: etag2,
        errorCode: null,
        durationMs: 18,
      },
      {
        stepKey: 'reject_stale_update',
        method: 'POST',
        path: `/api/v1/policies/${policyId}/cancel`,
        expectedStatus: 412,
        outcome: 'ExpectedPreconditionFailed',
        resultingStatus: 'Active',
        etag: etag1,
        errorCode: 'concurrency_conflict',
        durationMs: 11,
      },
      {
        stepKey: 'cancel_with_fresh_etag',
        method: 'POST',
        path: `/api/v1/policies/${policyId}/cancel`,
        expectedStatus: 200,
        outcome: 'Success',
        resultingStatus: 'Cancelled',
        etag: etag3,
        errorCode: null,
        durationMs: 16,
      },
      {
        stepKey: 'verify_transitions_audit',
        method: 'GET',
        path: `/api/v1/policies/${policyId}/transitions`,
        expectedStatus: 200,
        outcome: 'Success',
        resultingStatus: 'Cancelled',
        etag: etag3,
        errorCode: null,
        durationMs: 8,
      },
    ];

    const finalPolicy: PolicyResponse = {
      id: policyId,
      policyNumber,
      status: 'Cancelled',
      insuredAmount: 250000.0,
      currency: 'EUR',
      insuredPartyReference: 'CLI-84920412-ES',
      coverageStartDate: new Date(now.getTime() - 86400000 * 30).toISOString().split('T')[0],
      coverageEndDate: new Date(now.getTime() + 86400000 * 335).toISOString().split('T')[0],
      cancellationReason: 'Policyholder requested voluntary termination with mutual release agreement.',
      version: encodeRowVersion(3),
      createdAtUtc: new Date(now.getTime() - 100).toISOString(),
      updatedAtUtc: now.toISOString(),
    };

    const transList: PolicyTransitionResponse[] = [
      {
        id: crypto.randomUUID(),
        fromStatus: 'Draft',
        toStatus: 'Active',
        actor: 'system/public-demo',
        traceId: `0HN${Math.random().toString(36).substring(2, 9).toUpperCase()}:00000001`,
        reason: 'Policy activated with verified coverage dates and underwriting criteria.',
        createdAtUtc: new Date(now.getTime() - 50).toISOString(),
      },
      {
        id: crypto.randomUUID(),
        fromStatus: 'Active',
        toStatus: 'Cancelled',
        actor: 'system/public-demo',
        traceId: `0HN${Math.random().toString(36).substring(2, 9).toUpperCase()}:00000002`,
        reason: 'Policyholder requested voluntary termination with mutual release agreement.',
        createdAtUtc: now.toISOString(),
      },
    ];

    policies.set(policyId, { policy: finalPolicy, versionNum: 3 });
    transitions.set(policyId, transList);

    const totalDuration = steps.reduce((acc, s) => acc + s.durationMs, 0);

    steps.forEach((step) => {
      emitTelemetry(
        step.expectedStatus === 412 ? 'ConcurrencyGuard' : 'LifecycleCommand',
        `${step.method} ${step.path} (${step.stepKey})`,
        step.durationMs,
        step.expectedStatus,
        { etag: step.etag, outcome: step.outcome, resultingStatus: step.resultingStatus }
      );
    });

    return {
      runId,
      executedAtUtc: now.toISOString(),
      retentionHours: 24,
      totalDurationMs: totalDuration,
      cleanupCompleted: true,
      steps,
      finalPolicy,
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
        database: 'Healthy',
        engine: '.NET 10 Clean Architecture (In-Browser Execution)',
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
        policyNumber: `POL-2026-SYNTH`,
        status: 'Active',
        insuredAmount: 150000,
        currency: 'EUR',
        insuredPartyReference: 'REF-78901234',
        coverageStartDate: '2026-01-01',
        coverageEndDate: '2026-12-31',
        cancellationReason: null,
        version: encodeRowVersion(1),
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: new Date().toISOString(),
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
      policyNumber: req.policyNumber || `POL-2026-${String(policySeq++).padStart(4, '0')}`,
      status: 'Draft',
      insuredAmount: Number(req.insuredAmount) || 50000,
      currency: req.currency || 'EUR',
      insuredPartyReference: null,
      coverageStartDate: null,
      coverageEndDate: null,
      cancellationReason: null,
      version,
      createdAtUtc: now,
      updatedAtUtc: now,
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
    item.policy.updatedAtUtc = now;
    item.versionNum = nextVer;

    const trans = transitions.get(policyId) || [];
    trans.push({
      id: crypto.randomUUID(),
      fromStatus: 'Draft',
      toStatus: 'Active',
      actor: 'authenticated-operator',
      traceId: `0HN${Math.random().toString(36).substring(2, 9).toUpperCase()}:00000001`,
      reason: 'Standard underwriting activation.',
      createdAtUtc: now,
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
      });
    }

    const nextVer = item.versionNum + 1;
    const nextVerStr = encodeRowVersion(nextVer);
    const now = new Date().toISOString();

    const fromStatus = item.policy.status;
    item.policy.status = 'Cancelled';
    item.policy.cancellationReason = req.reason || 'Cancelled by operator request.';
    item.policy.version = nextVerStr;
    item.policy.updatedAtUtc = now;
    item.versionNum = nextVer;

    const trans = transitions.get(policyId) || [];
    trans.push({
      id: crypto.randomUUID(),
      fromStatus,
      toStatus: 'Cancelled',
      actor: 'authenticated-operator',
      traceId: `0HN${Math.random().toString(36).substring(2, 9).toUpperCase()}:00000002`,
      reason: req.reason,
      createdAtUtc: now,
    });
    transitions.set(policyId, trans);

    return { policy: item.policy, etag: `"${nextVerStr}"` };
  },
};
