<script setup lang="ts">
import { computed } from 'vue';
import { DOCS_URL } from '../services/api';
import { currentLocale, setLocale, t } from '../locales/i18n';

defineProps<{
  activeTab: string;
  hasToken: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:activeTab', tab: string): void;
  (e: 'openTokenModal'): void;
}>();

const tabs = computed(() => [
  { id: 'scenario', label: t('tabBlueprint') },
  { id: 'workbench', label: t('tabLifecycle') },
  { id: 'concurrency', label: t('tabConcurrency') },
  { id: 'audit', label: t('tabAudit') },
  { id: 'resilience', label: t('tabResilience') },
]);
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
          <span class="eyebrow">{{ t('eyebrow') }}</span>
        </div>
      </div>

      <div class="actions">
        <div class="lang-switch-group" role="group" aria-label="Language selector">
          <button
            type="button"
            class="lang-btn"
            :class="{ active: currentLocale === 'es' }"
            @click="setLocale('es')"
            aria-label="Español de España"
          >
            ES
          </button>
          <span class="lang-divider">|</span>
          <button
            type="button"
            class="lang-btn"
            :class="{ active: currentLocale === 'en' }"
            @click="setLocale('en')"
            aria-label="English"
          >
            EN
          </button>
        </div>

        <button type="button" class="btn btn-secondary token-btn" @click="emit('openTokenModal')">
          <span :class="['status-dot', hasToken ? 'on' : 'off']" aria-hidden="true"></span>
          <span>JWT</span>
        </button>
        <a :href="DOCS_URL" target="_blank" rel="noopener noreferrer" class="btn btn-secondary">{{ t('openApi') }}</a>
        <a href="https://github.com/Nain9Dev/API-Gestion-Financiera" target="_blank" rel="noopener noreferrer" class="btn btn-secondary">{{ t('sourceCode') }}</a>
      </div>
    </div>

    <nav class="tabs" aria-label="Sections">
      <button
        v-for="tabItem in tabs"
        :key="tabItem.id"
        type="button"
        :class="['tab', { active: activeTab === tabItem.id }]"
        :aria-current="activeTab === tabItem.id ? 'page' : undefined"
        @click="emit('update:activeTab', tabItem.id)"
      >
        {{ tabItem.label }}
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

.lang-switch-group {
  display: inline-flex;
  align-items: center;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border-subtle);
  border-radius: 4px;
  padding: 2px 6px;
  gap: 4px;
}

.lang-btn {
  background: none;
  border: none;
  color: var(--text-dim);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 2px;
  transition: all 0.15s ease;
}

.lang-btn:hover {
  color: #ffffff;
}

.lang-btn.active {
  color: var(--brand-green);
  background: rgba(128, 255, 0, 0.12);
}

.lang-divider {
  color: var(--border-subtle);
  font-size: 0.75rem;
  font-family: var(--font-mono);
  user-select: none;
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
