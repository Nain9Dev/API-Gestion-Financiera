/*
 * The backend runs on Azure App Service F1 with an Azure SQL Free database that auto-pauses.
 * A resume of tens of seconds is expected behaviour, not a fault, so this page probes readiness
 * before offering the run, reports elapsed time while waiting, and retries a bounded number of
 * times when the API reports 503.
 */

const READINESS_PROBE_TIMEOUT_MS = 15_000;
const READINESS_TOTAL_BUDGET_MS = 150_000;
const READINESS_RETRY_DELAY_MS = 4_000;
const RUN_TIMEOUT_MS = 120_000;
const RUN_MAX_ATTEMPTS = 3;

const runButton = document.querySelector("#run-demo");
const runStatus = document.querySelector("#run-status");
const backendState = document.querySelector("#backend-state");
const stepElements = [...document.querySelectorAll("#steps li")];
const rawResult = document.querySelector("#raw-result");
const rawJson = document.querySelector("#raw-json");
const metrics = document.querySelector("#run-metrics");
const metricRunId = document.querySelector("#metric-run-id");
const metricDuration = document.querySelector("#metric-duration");
const metricTransitions = document.querySelector("#metric-transitions");
const metricRetention = document.querySelector("#metric-retention");
const copyButton = document.querySelector("#copy-curl");
const curlCommand = document.querySelector("#curl-command");
const copyFeedback = document.querySelector("#copy-feedback");

const stepLabels = new Map([
  ["create_draft", "Crear Draft"],
  ["activate_policy", "Activar póliza"],
  ["reject_stale_update", "Rechazar ETag obsoleto"],
  ["cancel_policy", "Cancelar póliza"],
  ["read_audit_trail", "Leer auditoría"]
]);

let elapsedTimerId = null;

function delay(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function withTimeout(milliseconds) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), milliseconds);
  return { signal: controller.signal, clear: () => clearTimeout(timeoutId) };
}

function setBackendState(state, message) {
  backendState.dataset.state = state;
  backendState.textContent = message;
}

function setSteps(state, evidence) {
  stepElements.forEach((step) => {
    step.dataset.state = state;
    step.querySelector(".step-evidence").textContent = evidence;
  });
}

function startElapsedTimer(prefix) {
  const startedAt = Date.now();
  stopElapsedTimer();
  runStatus.textContent = `${prefix} (0 s)`;
  elapsedTimerId = setInterval(() => {
    const seconds = Math.round((Date.now() - startedAt) / 1000);
    runStatus.textContent = `${prefix} (${seconds} s)`;
  }, 1000);
}

function stopElapsedTimer() {
  if (elapsedTimerId !== null) {
    clearInterval(elapsedTimerId);
    elapsedTimerId = null;
  }
}

async function probeReadiness() {
  const timeout = withTimeout(READINESS_PROBE_TIMEOUT_MS);

  try {
    const response = await fetch("/health/ready", {
      method: "GET",
      cache: "no-store",
      signal: timeout.signal
    });
    return response.ok;
  } catch {
    return false;
  } finally {
    timeout.clear();
  }
}

async function waitForBackend() {
  const deadline = Date.now() + READINESS_TOTAL_BUDGET_MS;
  let attempt = 0;

  while (Date.now() < deadline) {
    attempt += 1;

    if (await probeReadiness()) {
      setBackendState("ready", "Backend y base de datos listos.");
      runButton.disabled = false;
      runButton.textContent = "Ejecutar demo ahora";
      return true;
    }

    setBackendState(
      "waking",
      `Despertando la base de datos del plan gratuito (Azure SQL AutoPause). Intento ${attempt}…`
    );
    await delay(READINESS_RETRY_DELAY_MS);
  }

  setBackendState(
    "unavailable",
    "El backend gratuito no ha respondido a tiempo. Puedes intentar la ejecución igualmente."
  );
  runButton.disabled = false;
  runButton.textContent = "Intentar de todas formas";
  return false;
}

function formatEvidence(step) {
  const parts = [`HTTP ${step.status}`];

  if (step.errorCode) {
    parts.push(step.errorCode);
  } else if (step.resourceStatus) {
    parts.push(step.resourceStatus);
  }

  if (typeof step.durationMs === "number") {
    parts.push(`${step.durationMs} ms`);
  }

  return parts.join(" · ");
}

function renderSteps(resultSteps) {
  resultSteps.forEach((result, index) => {
    const step = stepElements[index];

    if (!step) {
      return;
    }

    step.dataset.state = result.result === "unexpected_result" ? "error" : "success";
    step.querySelector("strong").textContent =
      stepLabels.get(result.operation) ?? result.operation;

    if (result.method && result.path) {
      step.querySelector(".step-route").textContent = `${result.method} ${result.path}`;
    }

    step.querySelector(".step-evidence").textContent = formatEvidence(result);
    step.title = result.etag ? `ETag: ${result.etag}` : "";
  });
}

function renderMetrics(result) {
  metricRunId.textContent = result.runId ?? "—";
  metricDuration.textContent =
    typeof result.totalDurationMs === "number" ? `${result.totalDurationMs} ms` : "—";
  metricTransitions.textContent = String(result.transitions?.length ?? 0);
  metricRetention.textContent = `${result.dataRetentionHours} h`;
  metrics.hidden = false;
}

async function readProblem(response) {
  try {
    return await response.json();
  } catch {
    return { detail: `La API respondió con HTTP ${response.status}.` };
  }
}

async function attemptRun() {
  const timeout = withTimeout(RUN_TIMEOUT_MS);

  try {
    const response = await fetch("/api/v1/demo/run", {
      method: "POST",
      cache: "no-store",
      headers: { Accept: "application/json" },
      signal: timeout.signal
    });

    if (response.status === 503) {
      return { retryable: true, message: "La base de datos gratuita sigue despertando." };
    }

    if (!response.ok) {
      const problem = await readProblem(response);
      return {
        retryable: false,
        message: problem.detail ?? `La API respondió con HTTP ${response.status}.`
      };
    }

    return { retryable: false, result: await response.json() };
  } catch (error) {
    const aborted = error instanceof DOMException && error.name === "AbortError";
    return {
      retryable: aborted,
      message: aborted
        ? "La ejecución superó el tiempo de espera mientras el plan gratuito arrancaba."
        : "No se pudo contactar con la API."
    };
  } finally {
    timeout.clear();
  }
}

async function runDemo() {
  runButton.disabled = true;
  runButton.textContent = "Ejecutando…";
  rawResult.hidden = true;
  metrics.hidden = true;
  setSteps("running", "En curso");
  startElapsedTimer("Ejecutando el flujo contra SQL Server");

  let lastMessage = "No se pudo ejecutar la demo.";

  for (let attempt = 1; attempt <= RUN_MAX_ATTEMPTS; attempt += 1) {
    const outcome = await attemptRun();

    if (outcome.result) {
      stopElapsedTimer();
      renderSteps(outcome.result.steps);
      renderMetrics(outcome.result);
      rawJson.textContent = JSON.stringify(outcome.result, null, 2);
      rawResult.hidden = false;
      setBackendState("ready", "Backend y base de datos listos.");
      runStatus.textContent =
        `Ejecución ${outcome.result.runId} completada en ${outcome.result.totalDurationMs} ms. ` +
        `Los datos sintéticos se conservan un máximo de ${outcome.result.dataRetentionHours} horas.`;
      runButton.disabled = false;
      runButton.textContent = "Ejecutar de nuevo";
      return;
    }

    lastMessage = outcome.message;

    if (!outcome.retryable || attempt === RUN_MAX_ATTEMPTS) {
      break;
    }

    setBackendState("waking", `${outcome.message} Reintentando (${attempt + 1}/${RUN_MAX_ATTEMPTS})…`);
    startElapsedTimer(`Reintento ${attempt + 1} de ${RUN_MAX_ATTEMPTS}`);
    await delay(READINESS_RETRY_DELAY_MS);
  }

  stopElapsedTimer();
  setSteps("error", "Sin ejecutar");
  setBackendState("unavailable", "El backend gratuito no está disponible ahora mismo.");
  runStatus.textContent = lastMessage;
  runButton.disabled = false;
  runButton.textContent = "Reintentar";
}

async function copyCurl() {
  const command = curlCommand.textContent.trim();

  try {
    await navigator.clipboard.writeText(command);
    copyFeedback.textContent = "Comando copiado al portapapeles.";
  } catch {
    copyFeedback.textContent = "Copia manualmente el comando: el navegador bloqueó el portapapeles.";
  }
}

runButton?.addEventListener("click", runDemo);
copyButton?.addEventListener("click", copyCurl);

waitForBackend();
