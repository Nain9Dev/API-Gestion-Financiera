<script setup lang="ts">
import { ref } from 'vue';
import { api, ApiError } from '../services/api';
import type { PublicDemoRunResponse } from '../types/contracts';

const isExecuting = ref(false);
const result = ref<PublicDemoRunResponse | null>(null);
const errorMessage = ref<string | null>(null);
const retryAfterSec = ref<number | null>(null);
const activeTabJson = ref<'policy' | 'transitions' | 'raw'>('policy');

async function handleRunScenario() {
  isExecuting.value = true;
  errorMessage.value = null;
  retryAfterSec.value = null;

  try {
    const data = await api.runPublicDemo();
    result.value = data;
  } catch (err: unknown) {
    if (err instanceof ApiError) {
      if (err.status === 429) {
        errorMessage.value = 'Rate limit reached: Public demo allows up to 10 requests per minute.';
        retryAfterSec.value = 60;
      } else if (err.status === 503) {
        errorMessage.value = 'Database service unavailable or resuming from cold-start AutoPause.';
      } else {
        errorMessage.value = err.message || `API error ${err.status}`;
      }
    } else {
      errorMessage.value = err instanceof Error ? err.message : String(err);
    }
  } finally {
    isExecuting.value = false;
  }
}
</script>

<template>
  <div class="scenario-view">
    <!-- Intro Card -->
    <div class="glass-panel intro-card">
      <div class="intro-content">
        <div class="badge-row">
          <span class="badge badge-cyan">Zero-Allocation Test Scenario</span>
          <span class="badge badge-emerald">Deterministic Proof</span>
          <span class="badge badge-slate">Synthetic Data Only</span>
        </div>
        <h2>One-Click Executable Scenario</h2>
        <p class="intro-description">
          Dispatches a certified lifecycle demonstration to the .NET 10 API engine.
          The transaction creates a policy draft, promotes it to active with an initial ETag, proves
          optimistic concurrency by rejecting a stale revision with <code>HTTP 412 Precondition Failed</code>,
          cancels it with the fresh version, and verifies the append-only transition log.
        </p>

        <div class="action-bar">
          <button
            type="button"
            class="btn btn-primary run-btn"
            :disabled="isExecuting"
            @click="handleRunScenario"
          >
            <span v-if="isExecuting" class="spinner"></span>
            <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polygon points="5 3 19 12 5 21 5 3"></polygon>
            </svg>
            <span>{{ isExecuting ? 'Executing Policy Transaction...' : 'Run Verified Scenario' }}</span>
          </button>

          <span class="endpoint-hint">
            Direct endpoint: <code>POST /api/v1/demo/run</code> (Rate limit: 10 req/min)
          </span>
        </div>
      </div>
    </div>

    <!-- Error Banner -->
    <div v-if="errorMessage" class="glass-panel error-card">
      <div class="error-icon">
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="12" y1="8" x2="12" y2="12"></line>
          <line x1="12" y1="16" x2="12.01" y2="16"></line>
        </svg>
      </div>
      <div>
        <h4>Execution Halt</h4>
        <p>{{ errorMessage }}</p>
        <p v-if="retryAfterSec" class="retry-hint">Retry-After window: {{ retryAfterSec }} seconds.</p>
      </div>
    </div>

    <!-- Results Section -->
    <div v-if="result" class="results-container">
      <!-- Metrics Bar -->
      <div class="metrics-grid">
        <div class="glass-panel metric-box">
          <span class="metric-label">Total Execution</span>
          <span class="metric-value highlight">{{ result.totalDurationMs }} ms</span>
          <span class="metric-sub">5 operations executed</span>
        </div>

        <div class="glass-panel metric-box">
          <span class="metric-label">Scenario Run ID</span>
          <span class="metric-value font-mono">{{ result.runId }}</span>
          <span class="metric-sub">Auto-pruned after {{ result.retentionHours }}h</span>
        </div>

        <div class="glass-panel metric-box">
          <span class="metric-label">Final Lifecycle Status</span>
          <span class="metric-value text-rose">{{ result.finalPolicy.status }}</span>
          <span class="metric-sub">Insured: {{ result.finalPolicy.insuredAmount }} {{ result.finalPolicy.currency }}</span>
        </div>

        <div class="glass-panel metric-box">
          <span class="metric-label">Audit History</span>
          <span class="metric-value text-emerald">{{ result.transitions.length }} Transitions</span>
          <span class="metric-sub">Append-only immutability</span>
        </div>
      </div>

      <!-- Step-by-Step Progress Waterfall -->
      <div class="glass-panel waterfall-panel">
        <div class="panel-header">
          <h3>Transaction Flow Breakdown</h3>
          <span class="badge badge-cyan">Certified Output</span>
        </div>

        <div class="steps-list">
          <div
            v-for="(step, idx) in result.steps"
            :key="step.stepKey"
            class="step-item"
          >
            <div class="step-num">{{ idx + 1 }}</div>
            <div class="step-body">
              <div class="step-header-row">
                <div class="step-title-wrap">
                  <span class="step-method" :class="'method-' + step.method.toLowerCase()">{{ step.method }}</span>
                  <span class="step-path font-mono">{{ step.path }}</span>
                </div>
                <div class="step-meta">
                  <span
                    :class="[
                      'badge',
                      step.expectedStatus === 200 || step.expectedStatus === 201 ? 'badge-emerald' : 'badge-amber'
                    ]"
                  >
                    HTTP {{ step.expectedStatus }}
                  </span>
                  <span class="step-time font-mono">{{ step.durationMs }} ms</span>
                </div>
              </div>

              <div class="step-details">
                <span class="step-outcome">
                  <strong>Outcome:</strong> {{ step.outcome }}
                </span>
                <span v-if="step.resultingStatus" class="step-status">
                  <strong>Status:</strong> {{ step.resultingStatus }}
                </span>
                <span v-if="step.etag" class="step-etag font-mono">
                  <strong>ETag:</strong> {{ step.etag }}
                </span>
                <span v-if="step.errorCode" class="step-error font-mono">
                  <strong>Error Code:</strong> {{ step.errorCode }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Inspection Tabs -->
      <div class="glass-panel inspection-panel">
        <div class="panel-header">
          <div class="tab-selectors">
            <button
              type="button"
              :class="['mini-tab', { active: activeTabJson === 'policy' }]"
              @click="activeTabJson = 'policy'"
            >
              Policy Snapshot
            </button>
            <button
              type="button"
              :class="['mini-tab', { active: activeTabJson === 'transitions' }]"
              @click="activeTabJson = 'transitions'"
            >
              Audit Trail ({{ result.transitions.length }})
            </button>
            <button
              type="button"
              :class="['mini-tab', { active: activeTabJson === 'raw' }]"
              @click="activeTabJson = 'raw'"
            >
              Full JSON Response
            </button>
          </div>
        </div>

        <div class="json-viewer">
          <pre v-if="activeTabJson === 'policy'"><code>{{ JSON.stringify(result.finalPolicy, null, 2) }}</code></pre>
          <pre v-else-if="activeTabJson === 'transitions'"><code>{{ JSON.stringify(result.transitions, null, 2) }}</code></pre>
          <pre v-else><code>{{ JSON.stringify(result, null, 2) }}</code></pre>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.scenario-view {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.intro-card {
  padding: 2rem;
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.85) 0%, rgba(12, 18, 32, 0.95) 100%);
  border: 1px solid var(--border-subtle);
}

.badge-row {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
  flex-wrap: wrap;
}

.intro-card h2 {
  font-family: var(--font-heading);
  font-size: 1.8rem;
  font-weight: 700;
  margin-bottom: 0.75rem;
}

.intro-description {
  color: var(--text-muted);
  max-width: 820px;
  line-height: 1.6;
  font-size: 0.95rem;
  margin-bottom: 1.5rem;
}

.intro-description code {
  background: rgba(255, 255, 255, 0.08);
  padding: 0.15rem 0.4rem;
  border-radius: var(--radius-sm);
  color: #7dd3fc;
  font-family: var(--font-mono);
}

.action-bar {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.run-btn {
  padding: 0.85rem 1.8rem;
  font-size: 1rem;
}

.endpoint-hint {
  font-size: 0.82rem;
  color: var(--text-dim);
}

.endpoint-hint code {
  font-family: var(--font-mono);
  color: var(--text-muted);
}

.error-card {
  padding: 1.25rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  border-left: 4px solid var(--accent-rose);
  background: rgba(244, 63, 94, 0.06);
}

.error-icon {
  color: var(--accent-rose);
}

.error-card h4 {
  color: #fca5a5;
  margin-bottom: 0.25rem;
}

.error-card p {
  color: var(--text-muted);
  font-size: 0.9rem;
}

.retry-hint {
  font-size: 0.82rem;
  color: #fcd34d;
  margin-top: 0.25rem;
}

.results-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.metric-box {
  padding: 1.25rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.metric-label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--text-dim);
}

.metric-value {
  font-size: 1.5rem;
  font-weight: 700;
  font-family: var(--font-heading);
}

.metric-value.highlight {
  color: #38bdf8;
}

.text-rose {
  color: var(--accent-rose);
}

.text-emerald {
  color: var(--accent-emerald);
}

.metric-sub {
  font-size: 0.78rem;
  color: var(--text-muted);
}

.waterfall-panel, .inspection-panel {
  padding: 1.5rem;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.25rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.panel-header h3 {
  font-family: var(--font-heading);
  font-size: 1.25rem;
  font-weight: 700;
}

.steps-list {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.step-item {
  display: flex;
  gap: 1rem;
  padding: 1rem;
  border-radius: var(--radius-sm);
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.04);
  transition: all 0.2s ease;
}

.step-item:hover {
  background: rgba(255, 255, 255, 0.04);
  border-color: rgba(255, 255, 255, 0.08);
}

.step-num {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: rgba(14, 165, 233, 0.15);
  color: #7dd3fc;
  font-weight: 700;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.step-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.step-header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.step-title-wrap {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.step-method {
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.15rem 0.45rem;
  border-radius: var(--radius-sm);
}

.method-post {
  background: rgba(14, 165, 233, 0.2);
  color: #38bdf8;
}

.method-get {
  background: rgba(16, 185, 129, 0.2);
  color: #34d399;
}

.step-path {
  font-size: 0.88rem;
  color: var(--text-main);
}

.step-meta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.step-time {
  font-size: 0.82rem;
  color: var(--text-dim);
}

.step-details {
  display: flex;
  flex-wrap: wrap;
  gap: 1.25rem;
  font-size: 0.82rem;
  color: var(--text-muted);
}

.step-etag strong, .step-status strong, .step-outcome strong, .step-error strong {
  color: var(--text-dim);
}

.tab-selectors {
  display: flex;
  gap: 0.5rem;
}

.mini-tab {
  padding: 0.4rem 0.85rem;
  font-size: 0.8rem;
  font-weight: 600;
  border-radius: var(--radius-sm);
  color: var(--text-muted);
  background: rgba(255, 255, 255, 0.04);
}

.mini-tab:hover {
  color: var(--text-main);
  background: rgba(255, 255, 255, 0.08);
}

.mini-tab.active {
  background: rgba(14, 165, 233, 0.2);
  color: #7dd3fc;
  border: 1px solid rgba(14, 165, 233, 0.35);
}

.json-viewer {
  background: #040810;
  border-radius: var(--radius-sm);
  padding: 1rem;
  overflow-x: auto;
  border: 1px solid rgba(255, 255, 255, 0.05);
}

.json-viewer pre {
  font-family: var(--font-mono);
  font-size: 0.82rem;
  color: #cbd5e1;
  line-height: 1.45;
}

.font-mono {
  font-family: var(--font-mono);
}
</style>
