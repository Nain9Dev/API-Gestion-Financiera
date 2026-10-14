<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import HeaderNav from './components/HeaderNav.vue';
import TokenModal from './components/TokenModal.vue';
import ScenarioView from './views/ScenarioView.vue';
import LifecycleView from './views/LifecycleView.vue';
import ConcurrencyLabView from './views/ConcurrencyLabView.vue';
import AuditTrailView from './views/AuditTrailView.vue';
import ResilienceProbeView from './views/ResilienceProbeView.vue';

const activeTab = ref('scenario');
const token = ref('');
const isTokenModalOpen = ref(false);

onMounted(() => {
  const saved = localStorage.getItem('nainpolicy_dev_jwt');
  if (saved) {
    token.value = saved;
  }
});

watch(token, (newVal) => {
  if (newVal) {
    localStorage.setItem('nainpolicy_dev_jwt', newVal);
  } else {
    localStorage.removeItem('nainpolicy_dev_jwt');
  }
});
</script>

<template>
  <div class="app-layout">
    <div class="content-container">
      <HeaderNav
        v-model:active-tab="activeTab"
        :has-token="Boolean(token)"
        @open-token-modal="isTokenModalOpen = true"
      />

      <main class="main-content">
        <ScenarioView v-if="activeTab === 'scenario'" />
        <LifecycleView
          v-else-if="activeTab === 'workbench'"
          :token="token"
          @open-token-modal="isTokenModalOpen = true"
        />
        <ConcurrencyLabView v-else-if="activeTab === 'concurrency'" />
        <AuditTrailView
          v-else-if="activeTab === 'audit'"
          :token="token"
          @open-token-modal="isTokenModalOpen = true"
        />
        <ResilienceProbeView v-else-if="activeTab === 'resilience'" />
      </main>

      <footer class="app-footer glass-panel">
        <div class="footer-left">
          <span class="footer-brand font-heading">NainPolicy</span>
          <span class="footer-sep">·</span>
          <span class="footer-meta">Financial & Insurance Policy Engine</span>
          <span class="footer-sep">·</span>
          <span class="footer-meta">.NET 10 Clean Architecture</span>
        </div>
        <div class="footer-right">
          <a href="http://localhost:8110/docs" target="_blank" rel="noopener noreferrer" class="footer-link">Scalar OpenApi</a>
          <span class="footer-sep">·</span>
          <a href="https://github.com/Nain9Dev/API-Gestion-Financiera" target="_blank" rel="noopener noreferrer" class="footer-link">GitHub Repository</a>
          <span class="footer-sep">·</span>
          <a href="https://naindev.com" target="_blank" rel="noopener noreferrer" class="footer-link highlight">naindev.com</a>
        </div>
      </footer>
    </div>

    <TokenModal
      v-model="token"
      :is-open="isTokenModalOpen"
      @close="isTokenModalOpen = false"
    />
  </div>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  padding: 1.5rem 1rem 3rem 1rem;
  display: flex;
  justify-content: center;
}

.content-container {
  width: 100%;
  max-width: 1200px;
  display: flex;
  flex-direction: column;
}

.main-content {
  flex: 1;
  margin-bottom: 2.5rem;
}

.app-footer {
  padding: 1.25rem 1.75rem;
  border-radius: var(--radius-md);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
  font-size: 0.82rem;
  color: var(--text-dim);
}

.footer-left {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.footer-brand {
  color: var(--text-main);
  font-weight: 700;
}

.footer-sep {
  color: rgba(255, 255, 255, 0.15);
}

.footer-right {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.footer-link {
  color: var(--text-muted);
  transition: color 0.2s;
}

.footer-link:hover {
  color: var(--text-main);
}

.footer-link.highlight {
  color: #38bdf8;
  font-weight: 600;
}

.font-heading {
  font-family: var(--font-heading);
}

@media (max-width: 768px) {
  .app-footer {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
