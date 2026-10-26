import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { PublicDemoRunResponse } from '../types/contracts';

const liveResponse: PublicDemoRunResponse = {
  runId: '06C3575F',
  executedAtUtc: '2026-09-08T14:06:22.887+00:00',
  dataRetentionHours: 24,
  totalDurationMs: 412,
  cleanupCompleted: true,
  steps: [
    {
      operation: 'create_draft',
      method: 'POST',
      path: '/api/v1/policies',
      status: 201,
      result: 'succeeded',
      resourceStatus: 'Draft',
      etag: '"AAAAAAAAH0E="',
      errorCode: null,
      durationMs: 118,
    },
  ],
  policy: {
    id: '9f1c7a52-0000-4000-8000-000000000001',
    organizationId: '33333333-3333-3333-3333-333333333333',
    policyNumber: 'DEMO-0001',
    insuredAmount: 1000,
    currency: 'EUR',
    insuredPartyReference: 'DEMO',
    coverageStartDate: '2026-01-01',
    coverageEndDate: '2026-12-31',
    status: 'Cancelled',
    createdAtUtc: '2026-09-08T14:06:22.887+00:00',
    version: 'AAAAAAAAH0M=',
  },
  transitions: [],
};

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

async function loadApi(baseUrl: string | undefined) {
  vi.resetModules();
  if (baseUrl === undefined) {
    vi.stubEnv('VITE_API_BASE_URL', '');
  } else {
    vi.stubEnv('VITE_API_BASE_URL', baseUrl);
  }
  return await import('./api');
}

describe('api.runScenario', () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  // A run starts the wake-up first when no earlier intent did (spec 005, REQ-142).
  const wakeUpOk = () => fetchMock.mockResolvedValueOnce(new Response('Healthy', { status: 200 }));

  it('labels a 200 from the configured API as live and passes the response through unchanged (REQ-110)', async () => {
    wakeUpOk();
    fetchMock.mockResolvedValueOnce(jsonResponse(liveResponse));
    const { api } = await loadApi('https://api.example.test');

    const run = await api.runScenario();

    expect(run.source).toBe('live');
    expect(run.response.steps[0].operation).toBe('create_draft');
    expect(run.response.policy.status).toBe('Cancelled');
  });

  it('prefixes the request with VITE_API_BASE_URL when it is configured (REQ-111)', async () => {
    wakeUpOk();
    fetchMock.mockResolvedValueOnce(jsonResponse(liveResponse));
    const { api } = await loadApi('https://api.example.test');

    await api.runScenario();

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][0]).toBe('https://api.example.test/health/ready');
    expect(fetchMock.mock.calls[1][0]).toBe('https://api.example.test/api/v1/demo/run');
  });

  it('derives the OpenAPI explorer link from the same base URL (REQ-111)', async () => {
    const configured = await loadApi('https://api.example.test/');
    expect(configured.DOCS_URL).toBe('https://api.example.test/docs');

    const local = await loadApi(undefined);
    expect(local.DOCS_URL).toBe('http://localhost:8110/docs');
  });

  it('keeps same-origin paths when VITE_API_BASE_URL is absent (REQ-111)', async () => {
    wakeUpOk();
    fetchMock.mockResolvedValueOnce(jsonResponse(liveResponse));
    const { api } = await loadApi(undefined);

    await api.runScenario();

    expect(fetchMock.mock.calls[0][0]).toBe('/health/ready');
    expect(fetchMock.mock.calls[1][0]).toBe('/api/v1/demo/run');
  });

  it('labels the in-browser fallback as simulated when the network fails (REQ-110)', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    const { api } = await loadApi('https://api.example.test');

    const run = await api.runScenario();

    expect(run.source).toBe('simulated');
    expect(run.response.steps).toHaveLength(5);
    expect(run.response.steps.map((s) => s.status)).toEqual([201, 200, 412, 200, 200]);
  });

  it('labels the fallback as simulated when the API answers with an error status (REQ-110)', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ code: 'service_unavailable' }, 503));
    const { api } = await loadApi('https://api.example.test');

    const run = await api.runScenario();

    expect(run.source).toBe('simulated');
  });

  it('keeps runPublicDemo as a thin alias returning only the response', async () => {
    wakeUpOk();
    fetchMock.mockResolvedValueOnce(jsonResponse(liveResponse));
    const { api } = await loadApi('https://api.example.test');

    const response = await api.runPublicDemo();

    expect(response.runId).toBe('06C3575F');
  });
});
