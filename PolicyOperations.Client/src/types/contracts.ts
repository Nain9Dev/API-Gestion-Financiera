export type PolicyStatus = 'Draft' | 'Active' | 'Cancelled';

export interface PolicyResponse {
  id: string;
  policyNumber: string;
  status: PolicyStatus;
  insuredAmount: number;
  currency: string;
  insuredPartyReference: string | null;
  coverageStartDate: string | null;
  coverageEndDate: string | null;
  cancellationReason: string | null;
  version: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PolicyTransitionResponse {
  id: string;
  fromStatus: PolicyStatus;
  toStatus: PolicyStatus;
  actor: string;
  traceId: string;
  reason: string | null;
  createdAtUtc: string;
}

export interface PublicDemoStepResponse {
  stepKey: string;
  method: string;
  path: string;
  expectedStatus: number;
  outcome: string;
  resultingStatus: string | null;
  etag: string | null;
  errorCode: string | null;
  durationMs: number;
}

export interface PublicDemoRunResponse {
  runId: string;
  executedAtUtc: string;
  retentionHours: number;
  totalDurationMs: number;
  cleanupCompleted: boolean;
  steps: PublicDemoStepResponse[];
  finalPolicy: PolicyResponse;
  transitions: PolicyTransitionResponse[];
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
