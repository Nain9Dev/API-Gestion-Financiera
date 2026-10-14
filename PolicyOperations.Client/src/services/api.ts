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
   * Runs the self-contained, validated end-to-end policy lifecycle scenario with cold-start timing.
   */
  async runPublicDemo(): Promise<PublicDemoRunResponse> {
    const res = await fetch(`${BASE_URL}/api/v1/demo/run`, {
      method: 'POST',
      headers: {
        'Accept': 'application/json',
      },
    });

    if (!res.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = await res.json();
      } catch {
        // Body was not JSON
      }
      throw new ApiError(res.status, problem);
    }

    return res.json();
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
      const durationMs = Math.round(performance.now() - start);
      let details: Record<string, unknown> | undefined;
      try {
        details = await res.json();
      } catch {
        // text or empty
      }

      const status = res.status === 200 ? 'Healthy' : res.status === 503 ? 'Degraded' : 'Unhealthy';
      return {
        status,
        statusCode: res.status,
        totalDurationMs: durationMs,
        timestamp: new Date().toISOString(),
        details,
      };
    } catch (err: unknown) {
      const durationMs = Math.round(performance.now() - start);
      return {
        status: 'Unhealthy',
        statusCode: 0,
        totalDurationMs: durationMs,
        timestamp: new Date().toISOString(),
        details: { error: err instanceof Error ? err.message : String(err) },
      };
    }
  },

  /**
   * Authenticated policy retrieval returning policy and ETag header.
   */
  async getPolicy(policyId: string, token: string): Promise<{ policy: PolicyResponse; etag: string | null }> {
    const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}`, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'Authorization': `Bearer ${token.trim()}`,
      },
    });

    if (!res.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = await res.json();
      } catch {
        // no problem details
      }
      throw new ApiError(res.status, problem);
    }

    const etag = res.headers.get('ETag');
    const policy: PolicyResponse = await res.json();
    return { policy, etag };
  },

  /**
   * Authenticated audit transitions retrieval.
   */
  async listTransitions(policyId: string, token: string): Promise<PolicyTransitionResponse[]> {
    const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}/transitions`, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'Authorization': `Bearer ${token.trim()}`,
      },
    });

    if (!res.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = await res.json();
      } catch {
        // no problem details
      }
      throw new ApiError(res.status, problem);
    }

    return res.json();
  },

  /**
   * Create Draft policy.
   */
  async createPolicy(req: CreatePolicyRequest, token: string): Promise<{ policy: PolicyResponse; etag: string | null }> {
    const res = await fetch(`${BASE_URL}/api/v1/policies`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'Authorization': `Bearer ${token.trim()}`,
      },
      body: JSON.stringify(req),
    });

    if (!res.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = await res.json();
      } catch {
        // ignore
      }
      throw new ApiError(res.status, problem);
    }

    const etag = res.headers.get('ETag');
    const policy: PolicyResponse = await res.json();
    return { policy, etag };
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
    const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}/activate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'If-Match': ifMatch.startsWith('"') ? ifMatch : `"${ifMatch}"`,
        'Authorization': `Bearer ${token.trim()}`,
      },
      body: JSON.stringify(req),
    });

    if (!res.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = await res.json();
      } catch {
        // ignore
      }
      throw new ApiError(res.status, problem);
    }

    const etag = res.headers.get('ETag');
    const policy: PolicyResponse = await res.json();
    return { policy, etag };
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
    const res = await fetch(`${BASE_URL}/api/v1/policies/${policyId}/cancel`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'If-Match': ifMatch.startsWith('"') ? ifMatch : `"${ifMatch}"`,
        'Authorization': `Bearer ${token.trim()}`,
      },
      body: JSON.stringify(req),
    });

    if (!res.ok) {
      let problem: ProblemDetails | undefined;
      try {
        problem = await res.json();
      } catch {
        // ignore
      }
      throw new ApiError(res.status, problem);
    }

    const etag = res.headers.get('ETag');
    const policy: PolicyResponse = await res.json();
    return { policy, etag };
  },
};
