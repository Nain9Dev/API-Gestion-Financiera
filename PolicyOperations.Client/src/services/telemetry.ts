import type { PublicDemoStepResponse } from '../types/contracts';

/**
 * Emits the `nain:telemetry` events consumed by the ecosystem bar's architecture drawer.
 * Safe to call outside a browser: it becomes a no-op.
 */
export function emitTelemetry(
  type: string,
  name: string,
  latencyMs: number,
  status: number,
  payload: unknown
): void {
  if (typeof window === 'undefined' || typeof CustomEvent === 'undefined') return;
  window.dispatchEvent(
    new CustomEvent('nain:telemetry', {
      detail: { type, name, latencyMs, status, payload },
    })
  );
}

export function emitScenarioTelemetry(
  steps: ReadonlyArray<PublicDemoStepResponse>,
  source: 'live' | 'simulated'
): void {
  for (const step of steps) {
    emitTelemetry(
      step.status === 412 ? 'ConcurrencyGuard' : step.method === 'GET' ? 'Query' : 'LifecycleCommand',
      `${step.method} ${step.path} (${step.operation})`,
      step.durationMs,
      step.status,
      { source, etag: step.etag, result: step.result, resourceStatus: step.resourceStatus }
    );
  }
}
