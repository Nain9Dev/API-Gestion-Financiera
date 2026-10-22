<script setup lang="ts">
import { ref } from 'vue';
import { api } from '../services/api';
import type { HealthCheckResult } from '../types/contracts';

const isProbingLiveness = ref(false);
const isProbingReadiness = ref(false);

const latestLiveness = ref<HealthCheckResult | null>(null);
const latestReadiness = ref<HealthCheckResult | null>(null);

interface ProbeLogEntry {
  id: string;
  target: 'Liveness (/health)' | 'SQL Readiness (/health/ready)';
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  statusCode: number;
  durationMs: number;
  timestamp: string;
}

const probeHistory = ref<ProbeLogEntry[]>([]);

async function probeLiveness() {
  isProbingLiveness.value = true;
  try {
    const res = await api.probeHealth('/health');
    latestLiveness.value = res;
    probeHistory.value.unshift({
      id: Math.random().toString(36).slice(2, 9),
      target: 'Liveness (/health)',
      status: res.status,
      statusCode: res.statusCode,
      durationMs: res.totalDurationMs,
      timestamp: new Date().toLocaleTimeString(),
    });
  } finally {
    isProbingLiveness.value = false;
  }
}

async function probeReadiness() {
  isProbingReadiness.value = true;
  try {
    const res = await api.probeHealth('/health/ready');
    latestReadiness.value = res;
    probeHistory.value.unshift({
      id: Math.random().toString(36).slice(2, 9),
      target: 'SQL Readiness (/health/ready)',
      status: res.status,
      statusCode: res.statusCode,
      durationMs: res.totalDurationMs,
      timestamp: new Date().toLocaleTimeString(),
    });
  } finally {
    isProbingReadiness.value = false;
  }
}
</script>

<template>
  <div class="resilience-view">
    <!-- Header card -->
    <div class="glass-panel info-card">
      <h2>Resilience probe</h2>
      <p class="description">Free tier with <code>AutoPause</code> · a cold wake-up is a <code>503 + Retry-After</code>, never a <code>500</code>.</p>

      <div class="probe-actions">
        <button
          type="button"
          class="btn btn-secondary"
          :disabled="isProbingLiveness"
          @click="probeLiveness"
        >
          <span v-if="isProbingLiveness" class="spinner"></span>
          <span>{{ isProbingLiveness ? 'Probing...' : 'Probe API Liveness (/health)' }}</span>
        </button>

        <button
          type="button"
          class="btn btn-primary"
          :disabled="isProbingReadiness"
          @click="probeReadiness"
        >
          <span v-if="isProbingReadiness" class="spinner"></span>
          <span>{{ isProbingReadiness ? 'Connecting to SQL...' : 'Probe SQL Server Readiness (/health/ready)' }}</span>
        </button>
      </div>
    </div>

    <!-- Active Probe Cards -->
    <div class="probe-cards-grid">
      <!-- Liveness Card -->
      <div class="glass-panel probe-card">
        <div class="card-head">
          <h4>API Liveness</h4>
          <span
            v-if="latestLiveness"
            :class="['badge', latestLiveness.status === 'Healthy' ? 'badge-emerald' : 'badge-rose']"
          >
            {{ latestLiveness.status }}
          </span>
          <span v-else class="badge badge-slate">Untested</span>
        </div>
        <p class="card-desc font-mono">GET /health</p>

        <div v-if="latestLiveness" class="metric-row">
          <span class="label">Roundtrip Latency:</span>
          <span class="value font-mono">{{ latestLiveness.totalDurationMs }} ms</span>
        </div>
        <div v-if="latestLiveness" class="metric-row">
          <span class="label">HTTP Status Code:</span>
          <span class="value font-mono">{{ latestLiveness.statusCode }}</span>
        </div>
      </div>

      <!-- Readiness Card -->
      <div class="glass-panel probe-card">
        <div class="card-head">
          <h4>SQL Server Readiness</h4>
          <span
            v-if="latestReadiness"
            :class="['badge', latestReadiness.status === 'Healthy' ? 'badge-emerald' : 'badge-rose']"
          >
            {{ latestReadiness.status }}
          </span>
          <span v-else class="badge badge-slate">Untested</span>
        </div>
        <p class="card-desc font-mono">GET /health/ready · SELECT 1</p>

        <div v-if="latestReadiness" class="metric-row">
          <span class="label">Roundtrip Latency:</span>
          <span class="value font-mono highlight">{{ latestReadiness.totalDurationMs }} ms</span>
        </div>
        <div v-if="latestReadiness" class="metric-row">
          <span class="label">HTTP Status Code:</span>
          <span class="value font-mono">{{ latestReadiness.statusCode }}</span>
        </div>
      </div>
    </div>

    <!-- Architectural Resilience Rules Grid -->
    <div class="glass-panel rules-panel">
      <div class="panel-header">
        <h3>ADR-006</h3>
        <span class="badge badge-emerald">Approved</span>
      </div>

      <div class="rules-grid">
        <div class="rule-item">
          <div class="rule-number">01</div>
          <div class="rule-body">
            <h4>Command timeout 60 s</h4>
          </div>
        </div>

        <div class="rule-item">
          <div class="rule-number">02</div>
          <div class="rule-body">
            <h4>503 + Retry-After</h4>
          </div>
        </div>

        <div class="rule-item">
          <div class="rule-number">03</div>
          <div class="rule-body">
            <h4>Non-fatal cleanup</h4>
          </div>
        </div>

        <div class="rule-item">
          <div class="rule-number">04</div>
          <div class="rule-body">
            <h4>0.00 € budget</h4>
          </div>
        </div>
      </div>
    </div>

    <!-- Telemetry History Log -->
    <div v-if="probeHistory.length > 0" class="glass-panel history-panel">
      <div class="panel-header">
        <h3>Probe Execution History</h3>
        <span class="sub-hint">{{ probeHistory.length }} recorded samples</span>
      </div>

      <div class="history-table-wrap">
        <table class="history-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Endpoint Probe</th>
              <th>Status</th>
              <th>HTTP Code</th>
              <th>Latency</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="entry in probeHistory" :key="entry.id">
              <td class="font-mono text-dim">{{ entry.timestamp }}</td>
              <td>{{ entry.target }}</td>
              <td>
                <span :class="['badge', entry.status === 'Healthy' ? 'badge-emerald' : 'badge-rose']">
                  {{ entry.status }}
                </span>
              </td>
              <td class="font-mono">{{ entry.statusCode }}</td>
              <td class="font-mono" :class="{ 'highlight': entry.durationMs > 1000 }">
                {{ entry.durationMs }} ms
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.resilience-view {
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

.probe-actions {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.probe-cards-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
}

.probe-card {
  padding: 1.5rem;
}

.card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.card-head h4 {
  font-family: var(--font-heading);
  font-size: 1.15rem;
  font-weight: 700;
}

.card-desc {
  font-size: 0.85rem;
  color: var(--text-muted);
  margin-bottom: 1.25rem;
  line-height: 1.5;
}

.card-desc code {
  font-family: var(--font-mono);
  color: #7dd3fc;
}

.metric-row {
  display: flex;
  justify-content: space-between;
  padding: 0.5rem 0;
  border-top: 1px solid rgba(255, 255, 255, 0.04);
  font-size: 0.88rem;
}

.metric-row .label {
  color: var(--text-dim);
}

.metric-row .value.highlight {
  color: #38bdf8;
  font-weight: 700;
}

.rules-panel, .history-panel {
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

.rules-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 1.25rem;
}

.rule-item {
  display: flex;
  gap: 1rem;
  padding: 1rem;
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.04);
  border-radius: var(--radius-sm);
}

.rule-number {
  font-family: var(--font-mono);
  font-size: 1.1rem;
  font-weight: 800;
  color: var(--accent-cyan);
}

.rule-body h4 {
  font-size: 0.95rem;
  margin-bottom: 0.35rem;
  color: var(--text-main);
}

.rule-body p {
  font-size: 0.82rem;
  color: var(--text-muted);
  line-height: 1.45;
}

.rule-body code {
  font-family: var(--font-mono);
  color: #7dd3fc;
}

.history-table-wrap {
  overflow-x: auto;
}

.history-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
  text-align: left;
}

.history-table th {
  padding: 0.65rem 1rem;
  color: var(--text-dim);
  font-size: 0.75rem;
  text-transform: uppercase;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.history-table td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.03);
}

.font-mono {
  font-family: var(--font-mono);
}

@media (max-width: 768px) {
  .probe-cards-grid {
    grid-template-columns: 1fr;
  }
}
</style>
