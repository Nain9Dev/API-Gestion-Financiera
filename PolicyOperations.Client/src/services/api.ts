import type {
  ActivatePolicyRequest,
  CancelPolicyRequest,
  CreatePolicyRequest,
  HealthCheckResult,
  PolicyResponse,
  PolicyTransitionResponse,
  ProblemDetails,
  PublicDemoRunResponse,
  ScenarioRun,
} from '../types/contracts';
import { mockPolicyEngine } from './mockPolicyEngine';
import { emitScenarioTelemetry } from './telemetry';

/**
 * Empty in development so the Vite proxy serves `/api` and `/health`.
 * The production build reads `.env.production` and targets the public API host.
 */
const BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '');

export const API_BASE_URL = BASE_URL;

/** Scalar OpenAPI explorer of the API this client talks to. */
export const DOCS_URL = BASE_URL ? `${BASE_URL}/docs` : 'http://localhost:8110/docs';

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

function now(): number {
  return typeof performance !== 'undefined' ? performance.now() : Date.now();
}

/** Longest a visitor waits for a live run before the in-browser result is shown (spec 004). */
const RUN_TIMEOUT_MS = 6_000;
/** The free-tier database can need up to two minutes to resume (spec 004). */
const WARM_UP_TIMEOUT_MS = 120_000;

export type WarmState = 'pending' | 'ready' | 'unavailable';

let warmState: WarmState | null = null;
let warmPromise: Promise<'ready' | 'unavailable'> | null = null;
const warmListeners = new Set<(state: WarmState) => void>();

function setWarmState(state: WarmState) {
  warmState = state;
  warmListeners.forEach((listener) => listener(state));
}

/** `fetch` that aborts after `timeoutMs`; the timer is cleared as soon as the call settles. */
async function fetchWithin(timeoutMs: number, input: string, init: RequestInit = {}): Promise<Response> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(input, { ...init, signal: controller.signal });
  } finally {
    clearTimeout(timer);
  }
}

export const api = {
  /**
   * Wakes the free-tier database as soon as the client loads. Sent once per page load; it never
   * substitutes a simulated health result, so `ready` always means the API answered 200.
   */
  warmUp(): Promise<'ready' | 'unavailable'> {
    if (!warmPromise) {
      setWarmState('pending');
      warmPromise = fetchWithin(WARM_UP_TIMEOUT_MS, `${BASE_URL}/health/ready`, { cache: 'no-store' })
        .then((res) => (res.ok ? ('ready' as const) : ('unavailable' as const)))
        .catch(() => 'unavailable' as const)
        .then((result) => {
          setWarmState(result);
          return result;
        });
    }
    return warmPromise;
  },

  getWarmState(): WarmState | null {
    return warmState;
  },

  /** Subscribes to warm-up changes; returns the unsubscribe function. */
  onWarmState(listener: (state: WarmState) => void): () => void {
    warmListeners.add(listener);
    return () => warmListeners.delete(listener);
  },

  /**
   * Runs the fixed five-step scenario against the configured API and says where the result came
   * from. When the API is unreachable or answers with an error, the in-browser engine produces the
   * same shape and the run is labelled `simulated`.
   */
  async runScenario(): Promise<ScenarioRun> {
    const started = now();
    try {
      const res = await fetchWithin(RUN_TIMEOUT_MS, `${BASE_URL}/api/v1/demo/run`, {
        method: 'POST',
        headers: { Accept: 'application/json' },
      });

      if (res.ok) {
        const response = (await res.json()) as PublicDemoRunResponse;
        emitScenarioTelemetry(response.steps, 'live');
        return { source: 'live', response, elapsedMs: Math.round(now() - started) };
      }
    } catch {
      // Backend unreachable or slower than RUN_TIMEOUT_MS: fall through to the in-browser engine.
    }

    const response = await mockPolicyEngine.runPublicDemo();
    emitScenarioTelemetry(response.steps, 'simulated');
    return { source: 'simulated', response, elapsedMs: Math.round(now() - started) };
  },

  /** Thin alias kept for views that only need the response. */
  async runPublicDemo(): Promise<PublicDemoRunResponse> {
    return (await api.runScenario()).response;
  },

  /**
   * Health probes with high-resolution client-side latency timing.
   */
  async probeHealth(endpoint: '/health' | '/health/ready'): Promise<HealthCheckResult> {
    const start = now();
    try {
      const res = await fetch(`${BASE_URL}${endpoint}`, {
        method: 'GET',
        cache: 'no-store',
      });
      if (res.ok) {
        const durationMs = Math.round(now() - start);
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
