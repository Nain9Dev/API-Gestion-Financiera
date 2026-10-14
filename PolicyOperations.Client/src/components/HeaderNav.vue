<script setup lang="ts">

defineProps<{
  activeTab: string;
  hasToken: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:activeTab', tab: string): void;
  (e: 'openTokenModal'): void;
}>();

const tabs = [
  { id: 'scenario', label: 'One-Click Scenario', badge: 'Verified' },
  { id: 'workbench', label: 'Policy Lifecycle', badge: 'Interactive' },
  { id: 'concurrency', label: 'Concurrency Lab', badge: 'HTTP 412' },
  { id: 'audit', label: 'Audit Trail', badge: 'Immutable' },
  { id: 'resilience', label: 'Resilience Probe', badge: 'Telemetry' },
];
</script>

<template>
  <header class="header glass-panel">
    <div class="top-row">
      <div class="brand-section">
        <div class="logo-box">
          <svg class="logo-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            <path d="m9 12 2 2 4-4"/>
          </svg>
        </div>
        <div>
          <div class="brand-title-wrap">
            <h1 class="gradient-title">NainPolicy</h1>
            <span class="badge badge-cyan">InsurTech Core</span>
            <span class="badge badge-slate">.NET 10</span>
          </div>
          <p class="tagline">Enterprise Policy Lifecycle, Optimistic Concurrency & Append-Only Audit Trail</p>
        </div>
      </div>

      <div class="actions-section">
        <button
          type="button"
          class="btn btn-secondary token-btn"
          @click="emit('openTokenModal')"
        >
          <span :class="['status-dot', hasToken ? 'connected' : 'disconnected']"></span>
          <span>{{ hasToken ? 'Dev JWT Active' : 'Configure JWT' }}</span>
        </button>

        <a href="http://localhost:8110/docs" target="_blank" rel="noopener noreferrer" class="btn btn-secondary nav-link-btn">
          Scalar Docs
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path>
            <polyline points="15 3 21 3 21 9"></polyline>
            <line x1="10" y1="14" x2="21" y2="3"></line>
          </svg>
        </a>

        <a href="https://naindev.com" target="_blank" rel="noopener noreferrer" class="btn btn-primary portfolio-btn">
          naindev.com
        </a>
      </div>
    </div>

    <!-- Navigation Tabs -->
    <nav class="nav-tabs" aria-label="Main Navigation">
      <button
        v-for="t in tabs"
        :key="t.id"
        :class="['tab-btn', { active: activeTab === t.id }]"
        @click="emit('update:activeTab', t.id)"
      >
        <span>{{ t.label }}</span>
        <span class="tab-badge" :class="activeTab === t.id ? 'active-badge' : ''">{{ t.badge }}</span>
      </button>
    </nav>
  </header>
</template>

<style scoped>
.header {
  margin-bottom: 2rem;
  border-radius: var(--radius-lg);
  padding: 1.25rem 1.75rem 0.5rem 1.75rem;
  border: 1px solid var(--border-subtle);
  background: linear-gradient(180deg, rgba(15, 23, 42, 0.9) 0%, rgba(12, 18, 32, 0.75) 100%);
}

.top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.brand-section {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.logo-box {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  background: linear-gradient(135deg, rgba(14, 165, 233, 0.2) 0%, rgba(99, 102, 241, 0.25) 100%);
  border: 1px solid rgba(14, 165, 233, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: var(--shadow-glow);
}

.logo-icon {
  width: 26px;
  height: 26px;
  color: #38bdf8;
}

.brand-title-wrap {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.brand-title-wrap h1 {
  font-size: 1.6rem;
  margin: 0;
  line-height: 1.2;
}

.tagline {
  font-size: 0.85rem;
  color: var(--text-muted);
  margin-top: 0.25rem;
}

.actions-section {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  display: inline-block;
}

.status-dot.connected {
  background: var(--accent-emerald);
  box-shadow: 0 0 8px var(--accent-emerald);
}

.status-dot.disconnected {
  background: var(--text-dim);
}

.nav-tabs {
  display: flex;
  gap: 0.5rem;
  overflow-x: auto;
  padding-top: 0.75rem;
}

.tab-btn {
  padding: 0.7rem 1.1rem;
  font-size: 0.88rem;
  font-weight: 600;
  color: var(--text-muted);
  border-bottom: 2px solid transparent;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.tab-btn:hover {
  color: var(--text-main);
  background: rgba(255, 255, 255, 0.03);
  border-radius: var(--radius-sm) var(--radius-sm) 0 0;
}

.tab-btn.active {
  color: #38bdf8;
  border-bottom-color: var(--accent-cyan);
}

.tab-badge {
  font-size: 0.68rem;
  padding: 0.15rem 0.45rem;
  border-radius: var(--radius-full);
  background: rgba(255, 255, 255, 0.06);
  color: var(--text-dim);
}

.active-badge {
  background: rgba(14, 165, 233, 0.18);
  color: #7dd3fc;
}

@media (max-width: 768px) {
  .top-row {
    flex-direction: column;
    align-items: flex-start;
  }
  .actions-section {
    width: 100%;
    justify-content: flex-start;
  }
}
</style>
