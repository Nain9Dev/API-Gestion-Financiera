/**
 * Wire contracts of PolicyOperations.Api. Field names mirror the C# records
 * (PolicyResponse, PolicyTransitionResponse, PublicDemoRunResponse, PublicDemoStepResponse)
 * serialised with camelCase and string enums.
 */

export type PolicyStatus = 'Draft' | 'Active' | 'Cancelled';

export interface PolicyResponse {
  id: string;
  organizationId: string;
  policyNumber: string;
  insuredAmount: number;
  currency: string;
  insuredPartyReference: string | null;
  coverageStartDate: string | null;
  coverageEndDate: string | null;
  status: PolicyStatus;
  createdAtUtc: string;
  version: string;
}

export interface PolicyTransitionResponse {
  id: string;
  policyId: string;
  fromStatus: PolicyStatus;
  toStatus: PolicyStatus;
  occurredAtUtc: string;
  actorSubject: string;
  reason: string | null;
  correlationId: string;
}

export type PublicDemoStepResult = 'succeeded' | 'rejected_as_expected';

export interface PublicDemoStepResponse {
  operation: string;
  method: string;
  path: string;
  status: number;
  result: PublicDemoStepResult;
  resourceStatus: string | null;
  etag: string | null;
  errorCode: string | null;
  durationMs: number;
}

export interface PublicDemoRunResponse {
  runId: string;
  executedAtUtc: string;
  dataRetentionHours: number;
  totalDurationMs: number;
  cleanupCompleted: boolean;
  steps: PublicDemoStepResponse[];
  policy: PolicyResponse;
  transitions: PolicyTransitionResponse[];
}

/** Where a scenario result came from. A simulated run is never presented as live evidence. */
export type ScenarioSource = 'live' | 'simulated';

export interface ScenarioRun {
  source: ScenarioSource;
  response: PublicDemoRunResponse;
  /** Wall-clock time observed by the browser, including any free-tier wake-up. */
  elapsedMs: number;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  [key: string]: unknown;
}

export interface CreatePolicyRequest {
  policyNumber: string;
  insuredAmount: number;
  currency: string;
}

export interface ActivatePolicyRequest {
  insuredPartyReference: string;
  coverageStartDate: string;
  coverageEndDate: string;
}

export interface CancelPolicyRequest {
  reason: string;
}

export interface HealthCheckResult {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  totalDurationMs: number;
  statusCode: number;
  timestamp: string;
  details?: Record<string, unknown>;
}
