<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import type { BlueprintFrame, EdgeId, NodeId } from '../architecture/blueprintTimeline';

export type BlueprintPhase = 'idle' | 'waiting' | 'replaying' | 'done' | 'error';

const props = defineProps<{
  frame: BlueprintFrame;
  visitedNodes: NodeId[];
  phase: BlueprintPhase;
  reducedMotion: boolean;
}>();

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

const NODES: Record<NodeId, Box> = {
  client: { x: 32, y: 196, w: 168, h: 128 },
  api: { x: 272, y: 96, w: 192, h: 328 },
  application: { x: 528, y: 96, w: 192, h: 328 },
  domain: { x: 784, y: 48, w: 192, h: 184 },
  infrastructure: { x: 784, y: 296, w: 192, h: 128 },
  sql: { x: 1024, y: 72, w: 140, h: 380 },
};

const EDGE_ORDER: EdgeId[] = [
  'client-api',
  'api-application',
  'application-domain',
  'application-infrastructure',
  'infrastructure-sql',
];

const EDGE_PATHS: Record<EdgeId, string> = {
  'client-api': 'M200 260 H272',
  'api-application': 'M464 260 H528',
  'application-domain': 'M720 200 C756 200 748 140 784 140',
  'application-infrastructure': 'M720 320 C756 320 748 360 784 360',
  'infrastructure-sql': 'M976 360 H1024',
};

const HOP_MS = 360;

const activeEdge = ref<EdgeId | null>(null);
const packetRefs = ref<Record<string, SVGAnimateMotionElement | null>>({});
let timers: number[] = [];

function clearTimers() {
  for (const t of timers) window.clearTimeout(t);
  timers = [];
  activeEdge.value = null;
}

function setPacketRef(edge: EdgeId, el: unknown) {
  packetRefs.value[edge] = (el as SVGAnimateMotionElement | null) ?? null;
}

function playPackets(edges: EdgeId[]) {
  clearTimers();
  if (props.reducedMotion || edges.length === 0) return;

  edges.forEach((edge, i) => {
    timers.push(
      window.setTimeout(() => {
        activeEdge.value = edge;
        const anim = packetRefs.value[edge];
        if (anim && typeof anim.beginElement === 'function') {
          anim.beginElement();
        }
      }, i * HOP_MS)
    );
  });
  timers.push(
    window.setTimeout(() => {
      activeEdge.value = null;
    }, edges.length * HOP_MS + 40)
  );
}

watch(
  () => props.frame,
  (frame) => {
    if (frame.index === 0) {
      clearTimers();
      return;
    }
    playPackets(frame.litEdges);
  }
);

onBeforeUnmount(clearTimers);

const isLit = (id: NodeId) => props.frame.litNodes.includes(id);
const isVisited = (id: NodeId) => props.visitedNodes.includes(id);
const edgeLit = (id: EdgeId) => props.frame.litEdges.includes(id);

const stateIndex: Record<string, number> = { Draft: 0, Active: 1, Cancelled: 2 };
const domainStates = ['Draft', 'Active', 'Cancelled'] as const;

function stateClass(state: string) {
  const current = stateIndex[props.frame.domainState];
  const mine = stateIndex[state];
  if (current === undefined) return 'state-off';
  if (mine === current) return 'state-current';
  if (mine < current) return 'state-visited';
  return 'state-off';
}

const ledgerRows = computed(() => [
  { id: 1, text: 'Draft → Active' },
  { id: 2, text: 'Active → Cancelled' },
]);

const versionLabel = computed(() => {
  const etag = props.frame.etag;
  if (!etag) return '—';
  const raw = etag.replace(/"/g, '');
  return raw.length > 6 ? `…${raw.slice(-6)}` : raw;
});

const description = computed(() => {
  const f = props.frame;
  if (f.index === 0) {
    return 'Request path from the client through the API, Application, Domain and Infrastructure layers to SQL Server. No run in progress.';
  }
  const where = f.tone === 'rejected' ? 'stopped at the concurrency guard in Application' : `reached ${f.litNodes[f.litNodes.length - 1]}`;
  return `Step ${f.index}, ${f.label}: ${f.method} ${f.path} returned ${f.status} in ${f.durationMs} ms and ${where}. Policy state ${f.domainState}, ${f.ledgerRows} audit rows.`;
});

const rootClass = computed(() => [
  'blueprint',
  `tone-${props.frame.tone}`,
  `phase-${props.phase}`,
  { 'reduced-motion': props.reducedMotion },
]);
</script>

<template>
  <svg
    :class="rootClass"
    viewBox="0 0 1200 500"
    role="img"
    aria-labelledby="bp-title bp-desc"
    preserveAspectRatio="xMidYMid meet"
  >
    <title id="bp-title">NainPolicy architecture blueprint</title>
    <desc id="bp-desc">{{ description }}</desc>

    <defs>
      <pattern id="bp-grid" width="40" height="40" patternUnits="userSpaceOnUse">
        <path d="M 40 0 L 0 0 0 40" fill="none" stroke="#0f2243" stroke-width="1" />
      </pattern>
      <pattern id="bp-grid-major" width="200" height="200" patternUnits="userSpaceOnUse">
        <path d="M 200 0 L 0 0 0 200" fill="none" stroke="#1b3b6f" stroke-width="1.2" />
      </pattern>
      <marker id="bp-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
        <path d="M 0 0 L 10 5 L 0 10 z" class="arrow-head" />
      </marker>
      <filter id="bp-glow" x="-30%" y="-30%" width="160%" height="160%">
        <feGaussianBlur stdDeviation="4" result="blur" />
        <feMerge>
          <feMergeNode in="blur" />
          <feMergeNode in="SourceGraphic" />
        </feMerge>
      </filter>
    </defs>

    <!-- Sheet -->
    <rect width="1200" height="500" fill="#060d1a" />
    <rect width="1200" height="500" fill="url(#bp-grid)" opacity="0.7" />
    <rect width="1200" height="500" fill="url(#bp-grid-major)" opacity="0.9" />
    <rect x="12" y="12" width="1176" height="476" fill="none" stroke="#1e3a8a" stroke-width="1.5" rx="3" />
    <line x1="12" y1="34" x2="1188" y2="34" stroke="#1b3b6f" stroke-width="1" stroke-dasharray="4 4" />
    <text x="24" y="27" class="sheet-label">NAINDEV ARCHITECTURAL BLUEPRINT // NAINPOLICY // CLEAN ARCHITECTURE</text>
    <text x="1176" y="27" class="sheet-meta" text-anchor="end">.NET 10 · EF CORE 10 · SQL SERVER · 52.0°</text>

    <!-- Edges -->
    <g class="edges">
      <path
        v-for="edge in EDGE_ORDER"
        :key="edge"
        :d="EDGE_PATHS[edge]"
        :class="['edge', `edge-${edge}`, { lit: edgeLit(edge), active: activeEdge === edge }]"
        marker-end="url(#bp-arrow)"
      />
      <!-- Reverse path for the rejected 412 -->
      <path
        d="M528 300 H464"
        class="edge edge-reject"
        :class="{ lit: frame.guardHit }"
        marker-end="url(#bp-arrow)"
      />
    </g>

    <!-- Packets -->
    <g class="packets">
      <circle
        v-for="edge in EDGE_ORDER"
        :key="`packet-${edge}`"
        r="6"
        :class="['packet', { travelling: activeEdge === edge }]"
      >
        <animateMotion
          :ref="(el) => setPacketRef(edge, el)"
          :path="EDGE_PATHS[edge]"
          :dur="`${HOP_MS}ms`"
          begin="indefinite"
          fill="freeze"
          calcMode="spline"
          keySplines="0.3 0 0.2 1"
        />
      </circle>
    </g>

    <!-- Client -->
    <g :class="['node', 'node-client', { lit: isLit('client'), visited: isVisited('client') }]">
      <rect :x="NODES.client.x" :y="NODES.client.y" :width="NODES.client.w" :height="NODES.client.h" class="node-box" rx="3" />
      <text :x="NODES.client.x + 16" :y="NODES.client.y + 30" class="node-title">Client</text>
      <text :x="NODES.client.x + 16" :y="NODES.client.y + 56" class="node-sub">browser · fetch · JSON</text>
      <text :x="NODES.client.x + 16" :y="NODES.client.y + 76" class="node-sub">no token · no body</text>
      <text :x="NODES.client.x + 16" :y="NODES.client.y + 108" class="node-tag">policy.naindev.com</text>
    </g>

    <!-- API -->
    <g :class="['node', 'node-api', { lit: isLit('api'), visited: isVisited('api') }]">
      <rect :x="NODES.api.x" :y="NODES.api.y" :width="NODES.api.w" :height="NODES.api.h" class="node-box" rx="3" />
      <text :x="NODES.api.x + 16" :y="NODES.api.y + 30" class="node-title">API</text>
      <text :x="NODES.api.x + 16" :y="NODES.api.y + 48" class="node-tag">PolicyOperations.Api</text>
      <g class="chips">
        <rect :x="NODES.api.x + 16" :y="NODES.api.y + 72" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.api.x + 26" :y="NODES.api.y + 89" class="chip-text">JWT · roles · org scope</text>
        <rect :x="NODES.api.x + 16" :y="NODES.api.y + 108" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.api.x + 26" :y="NODES.api.y + 125" class="chip-text">ETag / If-Match</text>
        <rect :x="NODES.api.x + 16" :y="NODES.api.y + 144" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.api.x + 26" :y="NODES.api.y + 161" class="chip-text">Problem Details</text>
        <rect :x="NODES.api.x + 16" :y="NODES.api.y + 180" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.api.x + 26" :y="NODES.api.y + 197" class="chip-text">429 · 503 + Retry-After</text>
      </g>
      <text :x="NODES.api.x + 16" :y="NODES.api.y + 300" class="node-sub">/api/v1 · OpenAPI</text>
    </g>

    <!-- Application -->
    <g :class="['node', 'node-application', { lit: isLit('application'), visited: isVisited('application'), guard: frame.guardHit }]">
      <rect :x="NODES.application.x" :y="NODES.application.y" :width="NODES.application.w" :height="NODES.application.h" class="node-box" rx="3" />
      <text :x="NODES.application.x + 16" :y="NODES.application.y + 30" class="node-title">Application</text>
      <text :x="NODES.application.x + 16" :y="NODES.application.y + 48" class="node-tag">PolicyService · ports</text>
      <g class="chips">
        <rect :x="NODES.application.x + 16" :y="NODES.application.y + 72" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.application.x + 26" :y="NODES.application.y + 89" class="chip-text">create / activate / cancel</text>
        <rect :x="NODES.application.x + 16" :y="NODES.application.y + 108" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.application.x + 26" :y="NODES.application.y + 125" class="chip-text">one UnitOfWork save</text>
      </g>
      <!-- Concurrency guard -->
      <g class="guard-box">
        <rect :x="NODES.application.x + 16" :y="NODES.application.y + 176" width="160" height="56" class="guard" rx="2" />
        <text :x="NODES.application.x + 26" :y="NODES.application.y + 198" class="guard-title">concurrency guard</text>
        <text :x="NODES.application.x + 26" :y="NODES.application.y + 218" class="guard-text">If-Match ≟ rowversion</text>
      </g>
      <g v-if="frame.guardHit" class="guard-burst">
        <circle :cx="NODES.application.x + 176" :cy="NODES.application.y + 176" r="20" class="burst" />
        <text :x="NODES.application.x + 176" :y="NODES.application.y + 181" class="burst-text" text-anchor="middle">412</text>
      </g>
      <text :x="NODES.application.x + 16" :y="NODES.application.y + 300" class="node-sub">no provider types</text>
    </g>

    <!-- Domain -->
    <g :class="['node', 'node-domain', { lit: isLit('domain'), visited: isVisited('domain') }]">
      <rect :x="NODES.domain.x" :y="NODES.domain.y" :width="NODES.domain.w" :height="NODES.domain.h" class="node-box" rx="3" />
      <text :x="NODES.domain.x + 16" :y="NODES.domain.y + 30" class="node-title">Domain</text>
      <text :x="NODES.domain.x + 16" :y="NODES.domain.y + 48" class="node-tag">lifecycle · invariants</text>
      <g class="states">
        <template v-for="(state, i) in domainStates" :key="state">
          <rect
            :x="NODES.domain.x + 16"
            :y="NODES.domain.y + 64 + i * 38"
            width="160"
            height="28"
            :class="['state', stateClass(state)]"
            rx="2"
          />
          <text :x="NODES.domain.x + 26" :y="NODES.domain.y + 83 + i * 38" :class="['state-text', stateClass(state)]">
            {{ state }}
          </text>
          <text
            v-if="i < 2"
            :x="NODES.domain.x + 170"
            :y="NODES.domain.y + 83 + i * 38"
            class="state-arrow"
            text-anchor="end"
          >
            ↓
          </text>
        </template>
      </g>
    </g>

    <!-- Infrastructure -->
    <g :class="['node', 'node-infrastructure', { lit: isLit('infrastructure'), visited: isVisited('infrastructure') }]">
      <rect :x="NODES.infrastructure.x" :y="NODES.infrastructure.y" :width="NODES.infrastructure.w" :height="NODES.infrastructure.h" class="node-box" rx="3" />
      <text :x="NODES.infrastructure.x + 16" :y="NODES.infrastructure.y + 30" class="node-title">Infrastructure</text>
      <text :x="NODES.infrastructure.x + 16" :y="NODES.infrastructure.y + 48" class="node-tag">EF Core 10</text>
      <g class="chips">
        <rect :x="NODES.infrastructure.x + 16" :y="NODES.infrastructure.y + 64" width="160" height="26" class="chip" rx="2" />
        <text :x="NODES.infrastructure.x + 26" :y="NODES.infrastructure.y + 81" class="chip-text">rowversion predicate</text>
        <rect :x="NODES.infrastructure.x + 16" :y="NODES.infrastructure.y + 96" width="160" height="22" class="chip" rx="2" />
        <text :x="NODES.infrastructure.x + 26" :y="NODES.infrastructure.y + 111" class="chip-text">transient retry · 60 s</text>
      </g>
    </g>

    <!-- SQL Server -->
    <g :class="['node', 'node-sql', { lit: isLit('sql'), visited: isVisited('sql') }]">
      <rect :x="NODES.sql.x" :y="NODES.sql.y" :width="NODES.sql.w" :height="NODES.sql.h" class="node-box" rx="3" />
      <ellipse :cx="NODES.sql.x + NODES.sql.w / 2" :cy="NODES.sql.y + 22" rx="44" ry="9" class="cylinder" />
      <text :x="NODES.sql.x + NODES.sql.w / 2" :y="NODES.sql.y + 52" class="node-title" text-anchor="middle">SQL Server</text>
      <text :x="NODES.sql.x + NODES.sql.w / 2" :y="NODES.sql.y + 70" class="node-tag" text-anchor="middle">Azure SQL · free</text>

      <!-- Policies table -->
      <g class="table">
        <rect :x="NODES.sql.x + 12" :y="NODES.sql.y + 92" width="112" height="22" class="table-head" rx="2" />
        <text :x="NODES.sql.x + 20" :y="NODES.sql.y + 107" class="table-title">Policies</text>
        <rect :x="NODES.sql.x + 12" :y="NODES.sql.y + 114" width="112" height="48" class="table-body" />
        <text :x="NODES.sql.x + 20" :y="NODES.sql.y + 132" class="table-cell">status {{ frame.domainState === 'None' ? '—' : frame.domainState }}</text>
        <text :x="NODES.sql.x + 20" :y="NODES.sql.y + 152" class="table-cell">rowversion {{ versionLabel }}</text>
      </g>

      <!-- Transitions ledger -->
      <g class="table ledger">
        <rect :x="NODES.sql.x + 12" :y="NODES.sql.y + 184" width="112" height="22" class="table-head" rx="2" />
        <text :x="NODES.sql.x + 20" :y="NODES.sql.y + 199" class="table-title">PolicyTransitions</text>
        <rect :x="NODES.sql.x + 12" :y="NODES.sql.y + 206" width="112" height="94" class="table-body" />
        <template v-for="row in ledgerRows" :key="row.id">
          <g :class="['ledger-row', { present: frame.ledgerRows >= row.id }]">
            <rect :x="NODES.sql.x + 16" :y="NODES.sql.y + 212 + (row.id - 1) * 30" width="104" height="24" class="ledger-cell" rx="2" />
            <text :x="NODES.sql.x + 22" :y="NODES.sql.y + 228 + (row.id - 1) * 30" class="ledger-text">{{ row.text }}</text>
          </g>
        </template>
        <text :x="NODES.sql.x + 20" :y="NODES.sql.y + 290" class="table-cell dim">append-only</text>
      </g>

      <text :x="NODES.sql.x + NODES.sql.w / 2" :y="NODES.sql.y + 340" class="node-sub" text-anchor="middle">1 transaction</text>
      <text :x="NODES.sql.x + NODES.sql.w / 2" :y="NODES.sql.y + 358" class="node-sub" text-anchor="middle">policy + audit row</text>
    </g>

    <!-- Status readout -->
    <g class="readout">
      <text x="24" y="478" class="sheet-meta">
        <template v-if="phase === 'waiting'">REQUEST IN FLIGHT · WAKING FREE-TIER DATABASE</template>
        <template v-else-if="frame.index > 0">STEP {{ String(frame.index).padStart(2, '0') }} · {{ frame.method }} {{ frame.path }} · {{ frame.status }} · {{ frame.durationMs }} ms</template>
        <template v-else>READY · RUN THE SCENARIO TO TRACE A REQUEST</template>
      </text>
      <text x="1176" y="478" class="sheet-meta" text-anchor="end">SPEC-002 · SHEET 1/1</text>
    </g>
  </svg>
</template>

<style scoped>
.blueprint {
  width: 100%;
  height: auto;
  display: block;
  font-family: var(--font-mono);
}

.sheet-label {
  font-size: 9.5px;
  font-weight: 600;
  letter-spacing: 0.28em;
  fill: var(--brand-green);
}

.sheet-meta {
  font-size: 9.5px;
  letter-spacing: 0.12em;
  fill: #94a3b8;
}

/* Nodes */
.node-box {
  fill: rgba(10, 22, 40, 0.92);
  stroke: #1b3b6f;
  stroke-width: 1.2;
  stroke-dasharray: 6 4;
  transition: stroke 0.25s ease, fill 0.25s ease, filter 0.25s ease;
}

.node.visited .node-box {
  stroke: #3c5f94;
  stroke-dasharray: none;
}

.node.lit .node-box {
  stroke: var(--brand-green);
  stroke-dasharray: none;
  fill: rgba(15, 34, 67, 0.96);
  filter: url(#bp-glow);
}

.tone-rejected .node.lit .node-box {
  stroke: var(--brand-red);
}

.node-title {
  font-family: var(--font-heading);
  font-size: 17px;
  font-weight: 800;
  fill: #ffffff;
}

.node-tag {
  font-size: 9.5px;
  letter-spacing: 0.1em;
  fill: var(--brand-green);
}

.node.lit .node-tag {
  fill: var(--brand-green);
}

.node-sub {
  font-size: 10px;
  fill: #94a3b8;
}

.chip {
  fill: rgba(0, 43, 102, 0.45);
  stroke: #1b3b6f;
  stroke-width: 1;
}

.node.lit .chip {
  stroke: rgba(128, 255, 0, 0.45);
}

.chip-text {
  font-size: 9.5px;
  fill: #cbd5e1;
}

/* Edges */
.edge {
  fill: none;
  stroke: #1b3b6f;
  stroke-width: 1.5;
  stroke-dasharray: 5 5;
  transition: stroke 0.25s ease, stroke-width 0.25s ease;
}

.arrow-head {
  fill: #1b3b6f;
}

.edge.lit {
  stroke: var(--brand-green);
  stroke-dasharray: none;
  stroke-width: 2;
}

.edge.active {
  stroke-width: 3;
}

.tone-rejected .edge.lit {
  stroke: var(--brand-red);
}

.edge-reject {
  opacity: 0;
  stroke: var(--brand-red);
}

.edge-reject.lit {
  opacity: 1;
  stroke-dasharray: none;
  stroke-width: 2;
}

.phase-waiting .edge-client-api {
  stroke: #38bdf8;
  stroke-dasharray: 8 6;
  animation: march 0.6s linear infinite;
}

@keyframes march {
  to { stroke-dashoffset: -14; }
}

/* Packets */
.packet {
  fill: var(--brand-green);
  opacity: 0;
  filter: url(#bp-glow);
}

.packet.travelling {
  opacity: 1;
}

.tone-rejected .packet {
  fill: var(--brand-red);
}

/* Guard */
.guard {
  fill: rgba(0, 43, 102, 0.35);
  stroke: #38bdf8;
  stroke-width: 1;
  stroke-dasharray: 3 3;
  transition: stroke 0.25s ease, fill 0.25s ease;
}

.guard-title {
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  fill: #7dd3fc;
}

.guard-text {
  font-size: 10.5px;
  fill: #e2e8f0;
}

.node.guard .guard {
  stroke: var(--brand-red);
  stroke-dasharray: none;
  fill: rgba(255, 0, 31, 0.14);
}

.burst {
  fill: var(--brand-red);
  stroke: #060d1a;
  stroke-width: 2;
  transform-box: fill-box;
  transform-origin: center;
  animation: pop 0.35s cubic-bezier(0.16, 1, 0.3, 1) both;
}

.burst-text {
  font-size: 11px;
  font-weight: 600;
  fill: #ffffff;
}

@keyframes pop {
  from { transform: scale(0.2); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

/* Domain states */
.state {
  fill: rgba(0, 43, 102, 0.3);
  stroke: #1b3b6f;
  stroke-width: 1;
  transition: fill 0.3s ease, stroke 0.3s ease;
}

.state.state-visited {
  stroke: #3c5f94;
  fill: rgba(60, 95, 148, 0.2);
}

.state.state-current {
  fill: var(--brand-green);
  stroke: var(--brand-green);
}

.state-text {
  font-family: var(--font-heading);
  font-size: 12px;
  font-weight: 700;
  fill: #64748b;
  transition: fill 0.3s ease;
}

.state-text.state-visited {
  fill: #cbd5e1;
}

.state-text.state-current {
  fill: var(--brand-black);
}

.state-arrow {
  font-size: 12px;
  fill: #3c5f94;
}

/* SQL */
.cylinder {
  fill: rgba(0, 43, 102, 0.6);
  stroke: #1b3b6f;
  stroke-width: 1.2;
}

.node.lit .cylinder {
  stroke: var(--brand-green);
}

.table-head {
  fill: rgba(0, 43, 102, 0.7);
  stroke: #1b3b6f;
}

.table-title {
  font-size: 9.5px;
  font-weight: 600;
  letter-spacing: 0.04em;
  fill: #ffffff;
}

.table-body {
  fill: rgba(6, 13, 26, 0.7);
  stroke: #1b3b6f;
  stroke-width: 1;
}

.table-cell {
  font-size: 9.5px;
  fill: #cbd5e1;
}

.table-cell.dim {
  fill: #64748b;
  letter-spacing: 0.08em;
}

.ledger-row {
  opacity: 0;
  transform: translateX(8px);
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.ledger-row.present {
  opacity: 1;
  transform: translateX(0);
}

.ledger-cell {
  fill: rgba(128, 255, 0, 0.12);
  stroke: rgba(128, 255, 0, 0.5);
}

.ledger-text {
  font-size: 9px;
  fill: var(--brand-green);
}

/* Reduced motion: no travelling packets, instant states */
.reduced-motion .packet,
.reduced-motion .phase-waiting .edge-client-api {
  animation: none;
}

.reduced-motion .node-box,
.reduced-motion .edge,
.reduced-motion .state,
.reduced-motion .ledger-row,
.reduced-motion .guard {
  transition: none;
}

.reduced-motion .burst {
  animation: none;
}
</style>
