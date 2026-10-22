<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api, ApiError } from '../services/api';
import type { PolicyTransitionResponse } from '../types/contracts';

const props = defineProps<{
  token: string;
}>();

const emit = defineEmits<{
  (e: 'openTokenModal'): void;
}>();

const policyIdInput = ref('');
const transitions = ref<PolicyTransitionResponse[]>([]);
const isLoading = ref(false);
const errorFeedback = ref<string | null>(null);

async function fetchFromDemo() {
  isLoading.value = true;
  errorFeedback.value = null;
  try {
    const run = await api.runPublicDemo();
    policyIdInput.value = run.policy.id;
    transitions.value = run.transitions;
  } catch (err: unknown) {
    if (err instanceof ApiError) {
      errorFeedback.value = err.problem?.detail || err.message;
    } else {
      errorFeedback.value = String(err);
    }
  } finally {
    isLoading.value = false;
  }
}

async function handleFetchCustom() {
  if (!policyIdInput.value.trim()) return;

  if (!props.token) {
    emit('openTokenModal');
    return;
  }

  isLoading.value = true;
  errorFeedback.value = null;
  try {
    const list = await api.listTransitions(policyIdInput.value.trim(), props.token);
    transitions.value = list;
  } catch (err: unknown) {
    if (err instanceof ApiError) {
      errorFeedback.value = err.problem?.detail || err.message;
    } else {
      errorFeedback.value = String(err);
    }
  } finally {
    isLoading.value = false;
  }
}

onMounted(() => {
  // Load default transitions from demo
  fetchFromDemo();
});
</script>

<template>
  <div class="audit-view">
    <!-- Header info card -->
    <div class="glass-panel info-card">
      <h2>Audit trail</h2>
      <p class="description">Append-only <code>PolicyTransitions</code> · actor, reason, correlation id, UTC.</p>

      <div class="search-bar">
        <div class="search-input-group">
          <label>Policy Identifier (GUID):</label>
          <input
            v-model="policyIdInput"
            type="text"
            class="input-field font-mono"
            placeholder="00000000-0000-0000-0000-000000000000"
          />
        </div>

        <button
          type="button"
          class="btn btn-secondary"
          :disabled="isLoading || !policyIdInput"
          @click="handleFetchCustom"
        >
          Query Policy History
        </button>

        <button
          type="button"
          class="btn btn-primary"
          :disabled="isLoading"
          @click="fetchFromDemo"
        >
          <span v-if="isLoading" class="spinner"></span>
          <span>{{ isLoading ? 'Retrieving Ledger...' : 'Load Verified Demo Ledger' }}</span>
        </button>
      </div>
    </div>

    <!-- Error banner -->
    <div v-if="errorFeedback" class="glass-panel error-card">
      <p>{{ errorFeedback }}</p>
    </div>

    <!-- Timeline Container -->
    <div class="glass-panel timeline-panel">
      <div class="panel-header">
        <div>
          <h3>Chronological Transition Sequence</h3>
          <span class="sub-hint">Total recorded state mutations: {{ transitions.length }}</span>
        </div>
        <span class="badge badge-emerald">Append-Only Verified</span>
      </div>

      <div v-if="transitions.length === 0 && !isLoading" class="empty-timeline">
        <p>No transition records found for the queried identifier.</p>
      </div>

      <div v-else class="timeline-container">
        <div
          v-for="(t, idx) in transitions"
          :key="t.id"
          class="timeline-item"
        >
          <div class="timeline-marker">
            <div class="marker-dot"></div>
            <div v-if="idx < transitions.length - 1" class="marker-line"></div>
          </div>

          <div class="timeline-content glass-panel">
            <div class="timeline-top">
              <div class="transition-pill-wrap">
                <span class="badge badge-slate font-mono">{{ t.fromStatus }}</span>
                <span class="arrow-symbol">→</span>
                <span
                  :class="[
                    'badge',
                    t.toStatus === 'Active' ? 'badge-emerald' : t.toStatus === 'Cancelled' ? 'badge-rose' : 'badge-cyan'
                  ]"
                >
                  {{ t.toStatus }}
                </span>
              </div>

              <span class="timestamp font-mono">{{ t.occurredAtUtc }}</span>
            </div>

            <div class="timeline-grid">
              <div class="meta-field">
                <span class="field-label">actor</span>
                <span class="field-value font-mono">{{ t.actorSubject }}</span>
              </div>
              <div class="meta-field">
                <span class="field-label">correlation</span>
                <span class="field-value font-mono">{{ t.correlationId }}</span>
              </div>
              <div v-if="t.reason" class="meta-field full-width">
                <span class="field-label">reason</span>
                <span class="field-value text-highlight">{{ t.reason }}</span>
              </div>
              <div class="meta-field full-width">
                <span class="field-label">id</span>
                <span class="field-value font-mono text-dim">{{ t.id }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.audit-view {
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

.search-bar {
  display: flex;
  align-items: flex-end;
  gap: 1rem;
  flex-wrap: wrap;
}

.search-input-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  flex: 1;
  min-width: 280px;
}

.search-input-group label {
  font-size: 0.8rem;
  color: var(--text-muted);
}

.input-field {
  padding: 0.65rem 0.85rem;
  border-radius: var(--radius-sm);
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid var(--border-subtle);
  color: var(--text-main);
  font-size: 0.88rem;
}

.input-field:focus {
  border-color: var(--accent-cyan);
  outline: none;
}

.error-card {
  padding: 1rem 1.25rem;
  background: rgba(244, 63, 94, 0.1);
  border-left: 4px solid var(--accent-rose);
  color: #fb7185;
}

.timeline-panel {
  padding: 1.5rem 2rem;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.panel-header h3 {
  font-family: var(--font-heading);
  font-size: 1.25rem;
  font-weight: 700;
}

.sub-hint {
  font-size: 0.8rem;
  color: var(--text-dim);
}

.empty-timeline {
  padding: 3rem 1rem;
  text-align: center;
  color: var(--text-dim);
}

.timeline-container {
  display: flex;
  flex-direction: column;
}

.timeline-item {
  display: flex;
  gap: 1.5rem;
  position: relative;
  padding-bottom: 1.75rem;
}

.timeline-marker {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 24px;
}

.marker-dot {
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background: var(--accent-emerald);
  box-shadow: 0 0 10px var(--accent-emerald);
  flex-shrink: 0;
  margin-top: 0.4rem;
}

.marker-line {
  width: 2px;
  flex: 1;
  background: rgba(16, 185, 129, 0.25);
  margin-top: 0.4rem;
}

.timeline-content {
  flex: 1;
  padding: 1.25rem 1.5rem;
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.05);
}

.timeline-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.transition-pill-wrap {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.arrow-symbol {
  color: var(--text-dim);
  font-weight: 700;
}

.timestamp {
  font-size: 0.8rem;
  color: var(--text-dim);
}

.timeline-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.85rem;
}

.meta-field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.meta-field.full-width {
  grid-column: 1 / -1;
}

.field-label {
  font-size: 0.75rem;
  color: var(--text-dim);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.field-value {
  font-size: 0.86rem;
  color: var(--text-main);
}

.text-highlight {
  color: #fcd34d;
}

.font-mono {
  font-family: var(--font-mono);
}

@media (max-width: 768px) {
  .timeline-grid {
    grid-template-columns: 1fr;
  }
}
</style>
