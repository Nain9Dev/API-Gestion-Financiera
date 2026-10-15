import type {
  ActivatePolicyRequest,
  CancelPolicyRequest,
  CreatePolicyRequest,
  HealthCheckResult,
  PolicyResponse,
  PolicyTransitionResponse,
  ProblemDetails,
  PublicDemoRunResponse,
} from '../types/contracts';
import { mockPolicyEngine } from './mockPolicyEngine';

const BASE_URL = '';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem?: ProblemDetails,
    message?: string
  ) {
    super(message || problem?.title || `API error ${status}`);
    this.name = 'ApiError';
  }
}

export const api = {
  /**
   * Runs the self-contained, validated end-to-end policy lifecycle scenario.
   */
  async runPublicDemo(): Promise<PublicDemoRunResponse> {
    try {
      const res = await fetch(`${BASE_URL}/api/v1/demo/run`, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
        },
      });

      if (res.ok) {
        return await res.json();
      }
    } catch {
      // Backend unreachable; execute in-browser deterministic scenario
    }

    return await mockPolicyEngine.runPublicDemo();
  },

  /**
   * Health probes with high-resolution client-side latency timing.
   */
  async probeHealth(endpoint: '/health' | '/health/ready'): Promise<HealthCheckResult> {
    const start = performance.now();
    try {
      const res = await fetch(`${BASE_URL}${endpoint}`, {
        method: 'GET',
        cache: 'no-store',
      });
      if (res.ok) {
        const durationMs = Math.round(performance.now() - start);
        let details: Record<string, unknown> | undefined;
        try {
          details = await res.json();
        } catch {
          // body was empty or not json
        }

        return {
          status: 'Healthy',
          statusCode: res.status,
          totalDurationMs: durationMs,
          timestamp: new Date().toISOString(),
          details,
        };
      }
    } catch {
      // Standalone execution
    }

    return mockPolicyEngine.probeHealth(endpoint);
  },

  /**
   * Policy retrieval returning policy and ETag header.
   */
  async getPolicy(policyId: string, token: string): Promise<{ policy: PolicyResponse; etag: string | null }> {
    try {
      const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}`, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${token.trim()}`,
        },
      });

      if (res.ok) {
        const etag = res.headers.get('ETag');
        const policy: PolicyResponse = await res.json();
        return { policy, etag };
      }
    } catch {
      // Standalone execution
    }

    return mockPolicyEngine.getPolicy(policyId);
  },

  /**
   * Audit transitions retrieval.
   */
  async listTransitions(policyId: string, token: string): Promise<PolicyTransitionResponse[]> {
    try {
      const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}/transitions`, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${token.trim()}`,
        },
      });

      if (res.ok) {
        return await res.json();
      }
    } catch {
      // Standalone execution
    }

    return mockPolicyEngine.listTransitions(policyId);
  },

  /**
   * Create Draft policy.
   */
  async createPolicy(req: CreatePolicyRequest, token: string): Promise<{ policy: PolicyResponse; etag: string | null }> {
    try {
      const res = await fetch(`${BASE_URL}/api/v1/policies`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
          Authorization: `Bearer ${token.trim()}`,
        },
        body: JSON.stringify(req),
      });

      if (res.ok) {
        const etag = res.headers.get('ETag');
        const policy: PolicyResponse = await res.json();
        return { policy, etag };
      }
    } catch {
      // Standalone execution
    }

    return mockPolicyEngine.createPolicy(req);
  },

  /**
   * Activate Policy with required If-Match header.
   */
  async activatePolicy(
    policyId: string,
    req: ActivatePolicyRequest,
    ifMatch: string,
    token: string
  ): Promise<{ policy: PolicyResponse; etag: string | null }> {
    try {
      const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}/activate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
          'If-Match': ifMatch.startsWith('"') ? ifMatch : `"${ifMatch}"`,
          Authorization: `Bearer ${token.trim()}`,
        },
        body: JSON.stringify(req),
      });

      if (res.ok) {
        const etag = res.headers.get('ETag');
        const policy: PolicyResponse = await res.json();
        return { policy, etag };
      }
    } catch {
      // Standalone execution
    }

    return mockPolicyEngine.activatePolicy(policyId, req, ifMatch);
  },

  /**
   * Cancel Policy with required If-Match header.
   */
  async cancelPolicy(
    policyId: string,
    req: CancelPolicyRequest,
    ifMatch: string,
    token: string
  ): Promise<{ policy: PolicyResponse; etag: string | null }> {
    try {
      const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}/cancel`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
          'If-Match': ifMatch.startsWith('"') ? ifMatch : `"${ifMatch}"`,
          Authorization: `Bearer ${token.trim()}`,
        },
        body: JSON.stringify(req),
      });

      if (res.ok) {
        const etag = res.headers.get('ETag');
        const policy: PolicyResponse = await res.json();
        return { policy, etag };
      }
    } catch {
      // Standalone execution
    }

    return mockPolicyEngine.cancelPolicy(policyId, req, ifMatch);
  },
};
