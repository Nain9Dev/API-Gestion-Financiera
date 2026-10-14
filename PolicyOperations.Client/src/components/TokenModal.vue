<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  isOpen: boolean;
  modelValue: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', token: string): void;
  (e: 'close'): void;
}>();

const tempToken = ref(props.modelValue);

function handleSave() {
  emit('update:modelValue', tempToken.value.trim());
  emit('close');
}

function handleClear() {
  tempToken.value = '';
  emit('update:modelValue', '');
  emit('close');
}
</script>

<template>
  <div v-if="isOpen" class="modal-backdrop" @click.self="emit('close')">
    <div class="glass-panel modal-card">
      <div class="modal-header">
        <div>
          <h3>Development JWT Configuration</h3>
          <p class="modal-subtitle">Local authorization token for custom mutations</p>
        </div>
        <button type="button" class="close-btn" @click="emit('close')">✕</button>
      </div>

      <div class="modal-body">
        <p class="help-text">
          Direct calls to <code>/api/v1/policies</code> require a validated JWT carrying claims
          <code>sub</code>, <code>organization_id</code> and role <code>PolicyOperator</code>.
        </p>

        <div class="form-group">
          <label>Bearer Token Value:</label>
          <textarea
            v-model="tempToken"
            class="token-textarea font-mono"
            rows="4"
            placeholder="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
          ></textarea>
        </div>

        <div class="helper-box">
          <span class="helper-title">Generate local token via CLI:</span>
          <pre class="font-mono cli-sample"><code>dotnet user-jwts create --project .\GestionFinanciera\PolicyOperations.Api\PolicyOperations.Api.csproj --name demo-operator --role PolicyOperator --role PolicyReader --claim organization_id=11111111-1111-1111-1111-111111111111 --valid-for 8h</code></pre>
        </div>
      </div>

      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" @click="handleClear">
          Clear Token
        </button>
        <button type="button" class="btn btn-primary" @click="handleSave">
          Save Token
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(4, 7, 14, 0.75);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-card {
  width: 100%;
  max-width: 620px;
  padding: 1.75rem;
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.95) 0%, rgba(10, 15, 28, 0.98) 100%);
  border: 1px solid var(--border-highlight);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.25rem;
}

.modal-header h3 {
  font-family: var(--font-heading);
  font-size: 1.3rem;
  font-weight: 700;
}

.modal-subtitle {
  font-size: 0.85rem;
  color: var(--text-muted);
  margin-top: 0.2rem;
}

.close-btn {
  font-size: 1.25rem;
  color: var(--text-dim);
  transition: color 0.2s;
}

.close-btn:hover {
  color: var(--text-main);
}

.modal-body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.help-text {
  font-size: 0.85rem;
  color: var(--text-muted);
  line-height: 1.5;
}

.help-text code {
  font-family: var(--font-mono);
  color: #7dd3fc;
  background: rgba(255, 255, 255, 0.08);
  padding: 0.1rem 0.35rem;
  border-radius: var(--radius-sm);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.form-group label {
  font-size: 0.8rem;
  color: var(--text-muted);
}

.token-textarea {
  width: 100%;
  padding: 0.75rem;
  border-radius: var(--radius-sm);
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid var(--border-subtle);
  color: var(--text-main);
  font-size: 0.82rem;
  resize: vertical;
}

.token-textarea:focus {
  border-color: var(--accent-cyan);
  outline: none;
}

.helper-box {
  padding: 1rem;
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.05);
  border-radius: var(--radius-sm);
}

.helper-title {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--text-dim);
  display: block;
  margin-bottom: 0.5rem;
}

.cli-sample {
  font-size: 0.75rem;
  color: #cbd5e1;
  background: #040810;
  padding: 0.75rem;
  border-radius: var(--radius-sm);
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-all;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

.font-mono {
  font-family: var(--font-mono);
}
</style>
