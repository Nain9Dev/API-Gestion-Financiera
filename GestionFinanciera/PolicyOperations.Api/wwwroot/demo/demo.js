const runButton = document.querySelector("#run-demo");
const runStatus = document.querySelector("#run-status");
const steps = [...document.querySelectorAll("#steps li")];
const rawResult = document.querySelector("#raw-result");
const rawJson = document.querySelector("#raw-json");

const labels = new Map([
  ["create_draft", "Crear Draft"],
  ["activate_policy", "Activar póliza"],
  ["reject_stale_update", "Rechazar ETag obsoleto"],
  ["cancel_policy", "Cancelar póliza"],
  ["read_audit_trail", "Leer auditoría"]
]);

function resetSteps() {
  steps.forEach((step) => {
    step.dataset.state = "pending";
    step.querySelector("small").textContent = "Pendiente";
  });
}

function renderSteps(resultSteps) {
  resultSteps.forEach((result, index) => {
    const step = steps[index];

    if (!step) {
      return;
    }

    step.dataset.state = result.result === "unexpected_result" ? "error" : "success";
    step.querySelector("strong").textContent = labels.get(result.operation) ?? result.operation;

    const evidence = result.errorCode
      ? `HTTP ${result.status} · ${result.errorCode}`
      : result.resourceStatus
        ? `HTTP ${result.status} · ${result.resourceStatus}`
        : `HTTP ${result.status} · 2 transiciones`;
    step.querySelector("small").textContent = evidence;
  });
}

async function readProblem(response) {
  try {
    return await response.json();
  } catch {
    return { detail: `La API respondió con HTTP ${response.status}.` };
  }
}

async function runDemo() {
  runButton.disabled = true;
  runButton.textContent = "Ejecutando…";
  runStatus.textContent = "La API está ejecutando el flujo contra SQL Server.";
  rawResult.hidden = true;
  resetSteps();

  try {
    const response = await fetch("/api/v1/demo/run", {
      method: "POST",
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      const problem = await readProblem(response);
      throw new Error(problem.detail ?? "No se pudo ejecutar la demo.");
    }

    const result = await response.json();
    renderSteps(result.steps);
    rawJson.textContent = JSON.stringify(result, null, 2);
    rawResult.hidden = false;
    runStatus.textContent = `Ejecución ${result.runId} completada. Los datos se conservarán un máximo de ${result.dataRetentionHours} horas.`;
  } catch (error) {
    runStatus.textContent = error instanceof Error
      ? error.message
      : "No se pudo ejecutar la demo.";
  } finally {
    runButton.disabled = false;
    runButton.textContent = "Ejecutar de nuevo";
  }
}

runButton?.addEventListener("click", runDemo);
