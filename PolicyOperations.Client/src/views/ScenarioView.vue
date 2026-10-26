<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import ArchitectureBlueprint, { type BlueprintPhase } from '../components/ArchitectureBlueprint.vue';
import { buildTimeline, IDLE_FRAME, replayDelayMs, type BlueprintFrame, type NodeId } from '../architecture/blueprintTimeline';
import { api, ApiError, type WarmState } from '../services/api';
import type { ScenarioRun } from '../types/contracts';

const phase = ref<BlueprintPhase>('idle');
const run = ref<ScenarioRun | null>(null);
const frames = ref<BlueprintFrame[]>([]);
const currentIndex = ref(0);
const errorMessage = ref<string | null>(null);
const elapsedMs = ref(0);
const showJson = ref(false);

const warmState = ref<WarmState | null>(api.getWarmState());
const stopWarmListener = api.onWarmState((state) => (warmState.value = state));

const reducedMotion = ref(false);
let motionQuery: MediaQueryList | null = null;
let elapsedTimer: number | null = null;
let replayTimers: number[] = [];

onMounted(() => {
  motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
  reducedMotion.value = motionQuery.matches;
  motionQuery.addEventListener('change', onMotionChange);
});

onBeforeUnmount(() => {
  stopWarmListener();
  motionQuery?.removeEventListener('change', onMotionChange);
  stopElapsed();
  clearReplay();
});

function onMotionChange(e: MediaQueryListEvent) {
  reducedMotion.value = e.matches;
}

function startElapsed() {
  const started = performance.now();
  elapsedMs.value = 0;
  elapsedTimer = window.setInterval(() => {
    elapsedMs.value = Math.round(performance.now() - started);
  }, 100);
}

function stopElapsed() {
  if (elapsedTimer !== null) {
    window.clearInterval(elapsedTimer);
    elapsedTimer = null;
  }
}

function clearReplay() {
  for (const t of replayTimers) window.clearTimeout(t);
  replayTimers = [];
}

const currentFrame = computed<BlueprintFrame>(() =>
  currentIndex.value > 0 && frames.value[currentIndex.value - 1]
    ? frames.value[currentIndex.value - 1]
    : IDLE_FRAME
);

const visitedNodes = computed<NodeId[]>(() => {
  const set = new Set<NodeId>();
  for (let i = 0; i < currentIndex.value - 1 && i < frames.value.length; i++) {
    for (const n of frames.value[i].litNodes) set.add(n);
  }
  return [...set];
});

const elapsedLabel = computed(() => `${(elapsedMs.value / 1000).toFixed(1)} s`);

async function handleRun() {
  if (phase.value === 'waiting' || phase.value === 'replaying') return;

  clearReplay();
  errorMessage.value = null;
  run.value = null;
  frames.value = [];
  currentIndex.value = 0;
  showJson.value = false;
  phase.value = 'waiting';
  startElapsed();

  try {
    const result = await api.runScenario();
    stopElapsed();
    elapsedMs.value = result.elapsedMs;
    run.value = result;
    frames.value = buildTimeline(result.response.steps);
    replay();
  } catch (err: unknown) {
    stopElapsed();
    phase.value = 'error';
    if (err instanceof ApiError) {
      errorMessage.value = err.status === 429 ? '429 · rate limit, retry in 60 s' : `${err.status} · ${err.problem?.code ?? err.message}`;
    } else {
      errorMessage.value = err instanceof Error ? err.message : String(err);
    }
  }
}

function replay() {
  phase.value = 'replaying';
  frames.value.forEach((_, i) => {
    replayTimers.push(
      window.setTimeout(() => {
        currentIndex.value = i + 1;
        if (i === frames.value.length - 1) {
          phase.value = 'done';
        }
      }, replayDelayMs(i, reducedMotion.value))
    );
  });
}

function showStep(index: number) {
  if (phase.value === 'replaying') return;
  currentIndex.value = index;
}

function statusClass(frame: BlueprintFrame) {
  if (frame.tone === 'rejected') return 'is-rejected';
  return 'is-ok';
}
</script>

<template>
  <section class="stage" aria-label="Live architecture blueprint">
    <div class="glass-panel sheet">
      <ArchitectureBlueprint
        :frame="currentFrame"
        :visited-nodes="visitedNodes"
        :phase="phase"
        :reduced-motion="reducedMotion"
      />
    </div>

    <div class="glass-panel control-strip">
      <button
        type="button"
        class="btn btn-primary run-btn"
        :disabled="phase === 'waiting' || phase === 'replaying'"
        @click="handleRun"
      >
        <span v-if="phase === 'waiting'" class="spinner" aria-hidden="true"></span>
        <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <polygon points="6 3 20 12 6 21 6 3"></polygon>
        </svg>
        <span>{{ phase === 'waiting' ? 'Running' : phase === 'replaying' ? 'Tracing' : run ? 'Run again' : 'Run scenario' }}</span>
      </button>

      <span class="readout font-mono" aria-live="polite">
        <template v-if="phase === 'waiting'">{{ elapsedLabel }}</template>
        <template v-else-if="run">{{ run.response.totalDurationMs }} ms server · {{ elapsedLabel }} wall</template>
        <template v-else>POST /api/v1/demo/run · 5 steps · no sign-up</template>
      </span>

      <span
        v-if="warmState === 'pending' || warmState === 'ready'"
        :class="['warm', `warm-${warmState}`]"
        role="status"
        aria-live="polite"
      >
        <span class="source-dot" aria-hidden="true"></span>
        {{ warmState === 'pending' ? 'Waking free-tier database' : 'Database ready' }}
      </span>

      <span v-if="run" :class="['source', `source-${run.source}`]" :title="run.source === 'live' ? 'Response from the API and SQL Server' : 'In-browser fallback: the API was unreachable'">
        <span class="source-dot" aria-hidden="true"></span>
        {{ run.source === 'live' ? 'Live · SQL Server' : 'Simulated · in-browser' }}
      </span>

      <span v-if="run" class="run-id font-mono">run {{ run.response.runId }}</span>

      <span v-if="errorMessage" class="error font-mono" role="alert">{{ errorMessage }}</span>

      <button
        v-if="run"
        type="button"
        class="btn btn-secondary json-btn"
        :aria-expanded="showJson"
        @click="showJson = !showJson"
      >
        JSON
      </button>
    </div>

    <ol v-if="frames.length" class="steps" aria-label="Scenario steps">
      <li v-for="frame in frames" :key="frame.index">
        <button
          type="button"
          :class="['step', statusClass(frame), { current: frame.index === currentIndex, reached: frame.index <= currentIndex }]"
          :aria-current="frame.index === currentIndex ? 'step' : undefined"
          :disabled="phase === 'replaying'"
          @click="showStep(frame.index)"
        >
          <span class="step-index font-mono">{{ String(frame.index).padStart(2, '0') }}</span>
          <span class="step-label">{{ frame.label }}</span>
          <span class="step-route font-mono">{{ frame.method }} {{ frame.path.replace(/\/api\/v1/, '').replace(/[0-9a-f-]{36}/, '{id}') }}</span>
          <span class="step-status font-mono">{{ frame.status }}</span>
          <span class="step-ms font-mono">{{ frame.durationMs }} ms</span>
        </button>
      </li>
    </ol>

    <div v-if="run && showJson" class="glass-panel json-panel">
      <pre class="font-mono"><code>{{ JSON.stringify(run.response, null, 2) }}</code></pre>
    </div>
  </section>
</template>

<style scoped>
.stage {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.sheet {
  padding: 0;
  overflow: hidden;
  line-height: 0;
}

.control-strip {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.6rem 0.75rem;
  flex-wrap: wrap;
}

.run-btn {
  padding: 0.65rem 1.4rem;
  font-size: 0.95rem;
}

.readout {
  font-size: 0.8rem;
  color: var(--text-muted);
  letter-spacing: 0.04em;
}

.source {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  font-family: var(--font-mono);
  font-size: 0.72rem;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  padding: 0.3rem 0.65rem;
  border-radius: var(--radius-sm);
  border: 1px solid;
}

.source-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: currentColor;
}

.source-live {
  color: var(--brand-green);
  border-color: rgba(128, 255, 0, 0.5);
  background: rgba(128, 255, 0, 0.08);
}

.source-live .source-dot {
  box-shadow: 0 0 10px var(--brand-green);
}

.source-simulated {
  color: #ffd27a;
  border-color: rgba(255, 176, 32, 0.5);
  background: rgba(255, 176, 32, 0.08);
}

.warm {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  font-family: var(--font-mono);
  font-size: 0.7rem;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--text-dim);
}

.warm-pending .source-dot {
  background: #ffd27a;
  animation: warm-pulse 1.2s ease-in-out infinite;
}

.warm-ready {
  color: var(--brand-green);
}

@keyframes warm-pulse {
  50% {
    opacity: 0.25;
  }
}

@media (prefers-reduced-motion: reduce) {
  .warm-pending .source-dot {
    animation: none;
  }
}

.run-id {
  font-size: 0.75rem;
  color: var(--text-dim);
}

.error {
  font-size: 0.78rem;
  color: #ff5c6e;
}

.json-btn {
  margin-left: auto;
  padding: 0.4rem 0.8rem;
  font-size: 0.75rem;
  font-family: var(--font-mono);
  letter-spacing: 0.1em;
}

.steps {
  list-style: none;
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.5rem;
}

.step {
  width: 100%;
  display: grid;
  grid-template-columns: auto 1fr auto;
  grid-template-areas:
    'index label status'
    'route route ms';
  align-items: center;
  column-gap: 0.6rem;
  row-gap: 0.3rem;
  padding: 0.6rem 0.75rem;
  text-align: left;
  background: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  opacity: 0.45;
  transition: opacity 0.2s ease, border-color 0.2s ease, transform 0.15s ease;
}

.step.reached {
  opacity: 1;
}

.step.current {
  border-color: var(--brand-green);
  box-shadow: inset 0 0 0 1px var(--brand-green);
}

.step.is-rejected.current {
  border-color: var(--brand-red);
  box-shadow: inset 0 0 0 1px var(--brand-red);
}

.step:not(:disabled):hover {
  transform: translateY(-1px);
  border-color: var(--accent-cyan);
}

.step:disabled {
  cursor: default;
}

.step-index {
  grid-area: index;
  font-size: 0.7rem;
  color: var(--text-dim);
}

.step-label {
  grid-area: label;
  font-family: var(--font-heading);
  font-weight: 800;
  font-size: 0.95rem;
  color: #ffffff;
}

.step-status {
  grid-area: status;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.1rem 0.4rem;
  border-radius: var(--radius-sm);
  color: var(--brand-black);
  background: var(--brand-green);
}

.is-rejected .step-status {
  background: var(--brand-red);
  color: #ffffff;
}

.step-route {
  grid-area: route;
  font-size: 0.68rem;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.step-ms {
  grid-area: ms;
  font-size: 0.7rem;
  color: var(--text-dim);
}

.json-panel {
  padding: 1rem;
  max-height: 420px;
  overflow: auto;
  background: #040810;
}

.json-panel pre {
  font-size: 0.78rem;
  color: #cbd5e1;
  line-height: 1.45;
}

@media (max-width: 960px) {
  .steps {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 640px) {
  /* Keep the sheet legible on a phone: scroll the blueprint sideways instead of shrinking it. */
  .sheet {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
  }

  .sheet :deep(svg) {
    min-width: 760px;
  }
}

@media (max-width: 560px) {
  .steps {
    grid-template-columns: 1fr;
  }

  .json-btn {
    margin-left: 0;
  }
}
</style>
