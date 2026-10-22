<script setup lang="ts">
import { ref } from 'vue';
import { api, ApiError } from '../services/api';
import type { PublicDemoRunResponse } from '../types/contracts';

const isRunning = ref(false);
const racePhase = ref<'idle' | 'read' | 'alpha_committed' | 'beta_rejected' | 'completed'>('idle');
const demoResult = ref<PublicDemoRunResponse | null>(null);
const errorMessage = ref<string | null>(null);

// Simulated / real state for the two operators
const sharedPolicyNumber = ref('SYNTH-CONCUR-2026');
const initialEtag = ref('"AAAAAAAAAAE="');
const alphaUpdatedEtag = ref('"AAAAAAAAAAI="');
const betaAttemptEtag = ref('"AAAAAAAAAAE="');

const betaProblemDetails = ref<Record<string, unknown>>({
  status: 412,
  title: 'Precondition Failed',
  detail: 'The requested policy version does not match the current stored state.',
  code: 'concurrency_conflict',
  traceId: '0HN0ABC123:00000001',
});

async function runRaceSimulation() {
  isRunning.value = true;
  racePhase.value = 'read';
  errorMessage.value = null;

  try {
    // Step 1: Initial state read
    await new Promise((r) => setTimeout(r, 600));
    racePhase.value = 'alpha_committed';

    // Step 2: Call the real backend demo scenario to get authoritative numbers and steps
    const res = await api.runPublicDemo();
    demoResult.value = res;

    // Extract actual ETags from backend steps
    const createStep = res.steps.find((s) => s.operation === 'create_draft');
    const activateStep = res.steps.find((s) => s.operation === 'activate_policy');
    const staleStep = res.steps.find((s) => s.operation === 'reject_stale_update');

    if (createStep?.etag) {
      initialEtag.value = createStep.etag;
      betaAttemptEtag.value = createStep.etag; // Stale ETag
    }
    if (activateStep?.etag) {
      alphaUpdatedEtag.value = activateStep.etag; // New ETag
    }
    if (staleStep) {
      betaProblemDetails.value = {
        status: staleStep.status,
        title: 'Precondition Failed',
        detail: 'The requested policy version does not match the current stored state.',
        code: staleStep.errorCode || 'concurrency_conflict',
        instance: staleStep.path,
        durationMs: staleStep.durationMs,
      };
    }

    await new Promise((r) => setTimeout(r, 800));
    racePhase.value = 'beta_rejected';

    await new Promise((r) => setTimeout(r, 800));
    racePhase.value = 'completed';
  } catch (err: unknown) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = String(err);
    }
    racePhase.value = 'idle';
  } finally {
    isRunning.value = false;
  }
}
</script>

<template>
  <div class="concurrency-lab">
    <!-- Lab Explanation Card -->
    <div class="glass-panel info-card">
      <h2>Concurrency lab</h2>
      <p class="description">Two operators, one <code>rowversion</code> · the stale <code>If-Match</code> gets <code>412</code>.</p>

      <div class="lab-controls">
        <button
          type="button"
          class="btn btn-primary start-btn"
          :disabled="isRunning"
          @click="runRaceSimulation"
        >
          <span v-if="isRunning" class="spinner"></span>
          <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polyline>
          </svg>
          <span>{{ isRunning ? 'Simulating Race Condition...' : 'Simulate Live Race Condition' }}</span>
        </button>

        <span class="phase-indicator">
          Current Phase: <strong>{{ racePhase.toUpperCase() }}</strong>
        </span>
      </div>
    </div>

    <!-- Error message if any -->
    <div v-if="errorMessage" class="glass-panel error-card">
      <p>{{ errorMessage }}</p>
    </div>

    <!-- The Two Operators Side by Side -->
    <div class="operators-grid">
      <!-- Operator Alpha Card -->
      <div class="glass-panel operator-card" :class="{ winner: racePhase === 'alpha_committed' || racePhase === 'completed' }">
        <div class="operator-header">
          <div class="avatar alpha-avatar">α</div>
          <div>
            <h3>Operator Alpha (Underwriter)</h3>
            <span class="operator-role">Policy Administration Terminal</span>
          </div>
          <span
            v-if="racePhase === 'alpha_committed' || racePhase === 'completed'"
            class="badge badge-emerald"
          >
            HTTP 200 OK
          </span>
        </div>

        <div class="card-timeline">
          <div class="timeline-step">
            <span class="step-badge">1</span>
            <div class="step-text">
              <strong>Reads Policy:</strong> {{ sharedPolicyNumber }}
              <div class="font-mono token-box">Holds ETag: {{ initialEtag }}</div>
            </div>
          </div>

          <div class="timeline-step" :class="{ active: racePhase === 'alpha_committed' || racePhase === 'completed' }">
            <span class="step-badge">2</span>
            <div class="step-text">
              <strong>Submits Lifecycle Update:</strong>
              <div class="font-mono request-sample">
                POST /policies/activate<br />
                If-Match: {{ initialEtag }}
              </div>
            </div>
          </div>

          <div class="timeline-step" :class="{ active: racePhase === 'alpha_committed' || racePhase === 'completed' }">
            <span class="step-badge">3</span>
            <div class="step-text">
              <strong>Database Mutation Accepted:</strong>
              <div class="status-box text-emerald">
                SQL Server rowversion incremented.<br />
                New ETag issued: <code>{{ alphaUpdatedEtag }}</code>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Operator Beta Card -->
      <div
        class="glass-panel operator-card"
        :class="{ conflict: racePhase === 'beta_rejected' || racePhase === 'completed' }"
      >
        <div class="operator-header">
          <div class="avatar beta-avatar">β</div>
          <div>
            <h3>Operator Beta (Claims Adjuster)</h3>
            <span class="operator-role">Claims & Settlement Terminal</span>
          </div>
          <span
            v-if="racePhase === 'beta_rejected' || racePhase === 'completed'"
            class="badge badge-rose"
          >
            HTTP 412 Conflict
          </span>
        </div>

        <div class="card-timeline">
          <div class="timeline-step">
            <span class="step-badge">1</span>
            <div class="step-text">
              <strong>Reads Same Policy:</strong> {{ sharedPolicyNumber }}
              <div class="font-mono token-box">Holds Stale ETag: {{ initialEtag }}</div>
            </div>
          </div>

          <div class="timeline-step" :class="{ active: racePhase === 'beta_rejected' || racePhase === 'completed' }">
            <span class="step-badge">2</span>
            <div class="step-text">
              <strong>Attempts Conflicting Update:</strong>
              <div class="font-mono request-sample">
                POST /policies/cancel<br />
                If-Match: {{ betaAttemptEtag }} <span class="stale-tag">[STALE]</span>
              </div>
            </div>
          </div>

          <div class="timeline-step" :class="{ active: racePhase === 'beta_rejected' || racePhase === 'completed' }">
            <span class="step-badge">3</span>
            <div class="step-text">
              <strong>Deterministic Rejection:</strong>
              <div class="status-box text-rose">
                HTTP 412 Precondition Failed.<br />
                Silent overwrite prevented.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- RFC 7807 Problem Details Inspector -->
    <div v-if="racePhase === 'beta_rejected' || racePhase === 'completed'" class="glass-panel payload-panel">
      <div class="panel-header">
        <div>
          <h3>RFC 7807 / 9457 Concurrency Conflict Payload</h3>
          <span class="sub-hint">Exact Problem Details body delivered to Operator Beta</span>
        </div>
        <span class="badge badge-rose">412 Precondition Failed</span>
      </div>

      <div class="json-code font-mono">
        <pre><code>{{ JSON.stringify(betaProblemDetails, null, 2) }}</code></pre>
      </div>
    </div>
  </div>
</template>

<style scoped>
.concurrency-lab {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.info-card {
  padding: 2rem;
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.85) 0%, rgba(12, 18, 32, 0.95) 100%);
}

.badge-row {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
  flex-wrap: wrap;
}

.info-card h2 {
  font-family: var(--font-heading);
  font-size: 1.8rem;
  font-weight: 700;
  margin-bottom: 0.75rem;
}

.description {
  color: var(--text-muted);
  max-width: 840px;
  line-height: 1.6;
  font-size: 0.95rem;
  margin-bottom: 1.5rem;
}

.description code {
  background: rgba(255, 255, 255, 0.08);
  padding: 0.15rem 0.4rem;
  border-radius: var(--radius-sm);
  color: #7dd3fc;
  font-family: var(--font-mono);
}

.lab-controls {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.start-btn {
  padding: 0.85rem 1.8rem;
  font-size: 0.95rem;
}

.phase-indicator {
  font-size: 0.85rem;
  color: var(--text-muted);
}

.phase-indicator strong {
  color: #38bdf8;
  font-family: var(--font-mono);
}

.error-card {
  padding: 1rem 1.25rem;
  background: rgba(244, 63, 94, 0.1);
  border-left: 4px solid var(--accent-rose);
  color: #fb7185;
}

.operators-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
}

.operator-card {
  padding: 1.5rem;
  border: 1px solid var(--border-subtle);
  transition: all 0.3s ease;
}

.operator-card.winner {
  border-color: var(--accent-emerald);
  box-shadow: 0 0 25px rgba(16, 185, 129, 0.15);
}

.operator-card.conflict {
  border-color: var(--accent-rose);
  box-shadow: 0 0 25px rgba(244, 63, 94, 0.15);
}

.operator-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  margin-bottom: 1.25rem;
}

.avatar {
  width: 42px;
  height: 42px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-heading);
  font-weight: 800;
  font-size: 1.2rem;
}

.alpha-avatar {
  background: rgba(14, 165, 233, 0.2);
  color: #38bdf8;
  border: 1px solid rgba(14, 165, 233, 0.4);
}

.beta-avatar {
  background: rgba(244, 63, 94, 0.2);
  color: #fb7185;
  border: 1px solid rgba(244, 63, 94, 0.4);
}

.operator-header h3 {
  font-family: var(--font-heading);
  font-size: 1.1rem;
  font-weight: 700;
}

.operator-role {
  font-size: 0.78rem;
  color: var(--text-dim);
}

.card-timeline {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.timeline-step {
  display: flex;
  gap: 0.85rem;
  opacity: 0.45;
  transition: all 0.3s ease;
}

.timeline-step.active {
  opacity: 1;
}

.step-badge {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  font-weight: 700;
  flex-shrink: 0;
}

.step-text {
  font-size: 0.85rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  flex: 1;
}

.token-box {
  background: rgba(255, 255, 255, 0.04);
  padding: 0.35rem 0.6rem;
  border-radius: var(--radius-sm);
  font-size: 0.8rem;
  color: #cbd5e1;
}

.request-sample {
  background: #040810;
  padding: 0.5rem 0.75rem;
  border-radius: var(--radius-sm);
  font-size: 0.78rem;
  color: #94a3b8;
  line-height: 1.4;
  border: 1px solid rgba(255, 255, 255, 0.05);
}

.stale-tag {
  color: #fb7185;
  font-weight: 700;
}

.status-box {
  padding: 0.45rem 0.75rem;
  border-radius: var(--radius-sm);
  font-size: 0.82rem;
  line-height: 1.4;
}

.status-box.text-emerald {
  background: rgba(16, 185, 129, 0.1);
  border: 1px solid rgba(16, 185, 129, 0.25);
  color: #6ee7b7;
}

.status-box.text-rose {
  background: rgba(244, 63, 94, 0.1);
  border: 1px solid rgba(244, 63, 94, 0.25);
  color: #fda4af;
}

.payload-panel {
  padding: 1.5rem;
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.9) 0%, rgba(12, 18, 32, 0.95) 100%);
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.panel-header h3 {
  font-family: var(--font-heading);
  font-size: 1.15rem;
  font-weight: 700;
}

.sub-hint {
  font-size: 0.8rem;
  color: var(--text-dim);
}

.json-code {
  background: #040810;
  padding: 1rem;
  border-radius: var(--radius-sm);
  font-size: 0.82rem;
  color: #cbd5e1;
  border: 1px solid rgba(255, 255, 255, 0.05);
  overflow-x: auto;
}

.explanation-box {
  margin-top: 1rem;
  padding: 1rem 1.25rem;
  background: rgba(14, 165, 233, 0.06);
  border-left: 3px solid var(--accent-cyan);
  border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
}

.explanation-box h4 {
  font-size: 0.9rem;
  color: #7dd3fc;
  margin-bottom: 0.25rem;
}

.explanation-box p {
  font-size: 0.84rem;
  color: var(--text-muted);
  line-height: 1.5;
}

.font-mono {
  font-family: var(--font-mono);
}

@media (max-width: 900px) {
  .operators-grid {
    grid-template-columns: 1fr;
  }
}
</style>
