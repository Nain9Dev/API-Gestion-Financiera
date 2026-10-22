<script setup lang="ts">
import { DOCS_URL } from '../services/api';

defineProps<{
  activeTab: string;
  hasToken: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:activeTab', tab: string): void;
  (e: 'openTokenModal'): void;
}>();

const tabs = [
  { id: 'scenario', label: 'Blueprint' },
  { id: 'workbench', label: 'Lifecycle' },
  { id: 'concurrency', label: 'Concurrency' },
  { id: 'audit', label: 'Audit' },
  { id: 'resilience', label: 'Resilience' },
];
</script>

<template>
  <header class="header glass-panel">
    <div class="top-row">
      <div class="brand">
        <svg class="logo" viewBox="0 0 440 440" aria-hidden="true">
          <defs>
            <mask id="logo-cut">
              <rect width="440" height="440" fill="white" />
              <polygon points="60,60 160,60 340,340 240,340" fill="none" stroke="black" stroke-width="24" stroke-linejoin="miter" />
            </mask>
          </defs>
          <g mask="url(#logo-cut)">
            <rect x="60" y="60" width="100" height="280" fill="#38BDF8" rx="8" />
            <rect x="240" y="60" width="100" height="280" fill="#38BDF8" rx="8" />
          </g>
          <polygon points="60,60 160,60 340,340 240,340" fill="#80FF00" />
        </svg>
        <div class="brand-text">
          <h1 class="gradient-title">NainPolicy</h1>
          <span class="eyebrow">Policy operations · .NET 10 · SQL Server</span>
        </div>
      </div>

      <div class="actions">
        <button type="button" class="btn btn-secondary token-btn" @click="emit('openTokenModal')">
          <span :class="['status-dot', hasToken ? 'on' : 'off']" aria-hidden="true"></span>
          <span>{{ hasToken ? 'JWT' : 'JWT' }}</span>
        </button>
        <a :href="DOCS_URL" target="_blank" rel="noopener noreferrer" class="btn btn-secondary">OpenAPI</a>
        <a href="https://github.com/Nain9Dev/API-Gestion-Financiera" target="_blank" rel="noopener noreferrer" class="btn btn-secondary">Source</a>
      </div>
    </div>

    <nav class="tabs" aria-label="Sections">
      <button
        v-for="t in tabs"
        :key="t.id"
        type="button"
        :class="['tab', { active: activeTab === t.id }]"
        :aria-current="activeTab === t.id ? 'page' : undefined"
        @click="emit('update:activeTab', t.id)"
      >
        {{ t.label }}
      </button>
    </nav>
  </header>
</template>

<style scoped>
.header {
  margin-bottom: 0.75rem;
  padding: 0.85rem 1rem 0 1rem;
}

.top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
  padding-bottom: 0.75rem;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.logo {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
}

.brand-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.brand-text h1 {
  font-size: 1.5rem;
  line-height: 1.1;
  margin: 0;
}

.actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.actions .btn {
  padding: 0.45rem 0.8rem;
  font-size: 0.78rem;
  font-family: var(--font-mono);
  letter-spacing: 0.06em;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  display: inline-block;
}

.status-dot.on {
  background: var(--brand-green);
  box-shadow: 0 0 8px var(--brand-green);
}

.status-dot.off {
  background: var(--text-dim);
}

.tabs {
  display: flex;
  gap: 0.25rem;
  overflow-x: auto;
  border-top: 1px solid var(--border-subtle);
}

.tab {
  padding: 0.7rem 0.9rem;
  font-family: var(--font-heading);
  font-size: 0.9rem;
  font-weight: 700;
  color: var(--text-muted);
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  transition: color 0.15s ease, border-color 0.15s ease;
  white-space: nowrap;
}

.tab:hover {
  color: #ffffff;
}

.tab.active {
  color: var(--brand-green);
  border-bottom-color: var(--brand-green);
}

@media (max-width: 640px) {
  .top-row {
    align-items: flex-start;
  }

  .actions {
    width: 100%;
  }
}
</style>
