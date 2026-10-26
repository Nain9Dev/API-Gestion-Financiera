import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

async function loadApi(baseUrl = 'https://api.example.test') {
  vi.resetModules();
  vi.stubEnv('VITE_API_BASE_URL', baseUrl);
  return await import('./api');
}

/** A fetch that never answers on its own and rejects once its signal aborts. */
function hangingFetch(): typeof fetch {
  return ((_input: RequestInfo | URL, init?: RequestInit) =>
    new Promise<Response>((_resolve, reject) => {
      init?.signal?.addEventListener('abort', () =>
        reject(new DOMException('aborted', 'AbortError'))
      );
    })) as typeof fetch;
}

const demoOk = {
  runId: 'AAAA0000',
  executedAtUtc: '2026-10-26T10:00:00+00:00',
  dataRetentionHours: 24,
  totalDurationMs: 40,
  cleanupCompleted: true,
  steps: [],
  policy: {},
  transitions: [],
};

describe('api.warmUp (spec 004)', () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it('sends one GET /health/ready to the configured API (REQ-130)', async () => {
    fetchMock.mockResolvedValueOnce(new Response('Healthy', { status: 200 }));
    const { api } = await loadApi();

    await api.warmUp();

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock.mock.calls[0][0]).toBe('https://api.example.test/health/ready');
    expect(fetchMock.mock.calls[0][1]?.method ?? 'GET').toBe('GET');
  });

  it('sends the wake-up at most once however often it is requested (REQ-131)', async () => {
    fetchMock.mockResolvedValue(new Response('Healthy', { status: 200 }));
    const { api } = await loadApi();

    await Promise.all([api.warmUp(), api.warmUp()]);
    await api.warmUp();

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('reports ready when the API answers 200 (REQ-132)', async () => {
    fetchMock.mockResolvedValueOnce(new Response('Healthy', { status: 200 }));
    const { api } = await loadApi();

    expect(await api.warmUp()).toBe('ready');
    expect(api.getWarmState()).toBe('ready');
  });

  it('reports unavailable on an error status without inventing a result (REQ-132)', async () => {
    fetchMock.mockResolvedValueOnce(new Response('{}', { status: 503 }));
    const { api } = await loadApi();

    expect(await api.warmUp()).toBe('unavailable');
    expect(api.getWarmState()).toBe('unavailable');
  });

  it('reports unavailable when the network fails (REQ-132)', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    const { api } = await loadApi();

    expect(await api.warmUp()).toBe('unavailable');
  });

  it('gives up after 120 s and reports unavailable (REQ-132)', async () => {
    vi.useFakeTimers();
    fetchMock.mockImplementation(hangingFetch());
    const { api } = await loadApi();

    const pending = api.warmUp();
    expect(api.getWarmState()).toBe('pending');
    await vi.advanceTimersByTimeAsync(120_000);

    expect(await pending).toBe('unavailable');
  });

  it('notifies subscribers so the view can show the waking status (REQ-133)', async () => {
    fetchMock.mockResolvedValueOnce(new Response('Healthy', { status: 200 }));
    const { api } = await loadApi();
    const seen: string[] = [];
    const stop = api.onWarmState((s) => seen.push(s));

    await api.warmUp();
    stop();

    expect(seen).toEqual(['pending', 'ready']);
  });
});

describe('api.runScenario time limit (spec 004)', () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it('aborts after 6 s and answers with a run labelled simulated (REQ-134)', async () => {
    vi.useFakeTimers();
    fetchMock.mockImplementation(hangingFetch());
    const { api } = await loadApi();

    const pending = api.runScenario();
    await vi.advanceTimersByTimeAsync(6_000);
    const run = await pending;

    expect(run.source).toBe('simulated');
  });

  it('keeps waiting while the answer is still inside the limit (REQ-134)', async () => {
    vi.useFakeTimers();
    let release!: (r: Response) => void;
    fetchMock.mockImplementation(
      () => new Promise<Response>((resolve) => (release = resolve))
    );
    const { api } = await loadApi();

    const pending = api.runScenario();
    await vi.advanceTimersByTimeAsync(5_900);
    release(new Response(JSON.stringify(demoOk), { status: 200 }));
    const run = await pending;

    expect(run.source).toBe('live');
  });

  it('labels a fast answer live (REQ-135)', async () => {
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify(demoOk), { status: 200 }));
    const { api } = await loadApi();

    expect((await api.runScenario()).source).toBe('live');
  });

  it('does not wait for a pending wake-up before running (REQ-136)', async () => {
    vi.useFakeTimers();
    fetchMock
      .mockImplementationOnce(hangingFetch()) // warm-up stays pending
      .mockResolvedValueOnce(new Response(JSON.stringify(demoOk), { status: 200 })); // run
    const { api } = await loadApi();

    void api.warmUp();
    const run = await api.runScenario();

    expect(run.source).toBe('live');
    expect(fetchMock.mock.calls[1][0]).toBe('https://api.example.test/api/v1/demo/run');
  });
});
