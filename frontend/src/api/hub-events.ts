export type PipelineUpdated = {
  pipelineId: string;
  action: "created" | "updated" | "deleted";
};

export type RunStateChanged = {
  runId: string;
  pipelineId: string;
  status: "pending" | "running" | "success" | "failed";
  startedAt?: string;
  finishedAt?: string;
  errorMessage?: string;
};

export type StepStateChanged = {
  runId: string;
  pipelineId: string;
  stepName: string;
  status: "pending" | "running" | "success" | "failed";
  startedAt?: string;
  finishedAt?: string;
};

export type AlertRaised = {
  alertId: string;
  ruleId: string;
  runId: string;
  pipelineId: string;
  message: string;
  severity: "info" | "warning" | "error";
  createdAt: string;
};

export type HubEventName =
  | "PipelineUpdated"
  | "RunStateChanged"
  | "StepStateChanged"
  | "AlertRaised";

export type HubEventPayload = {
  PipelineUpdated: PipelineUpdated;
  RunStateChanged: RunStateChanged;
  StepStateChanged: StepStateChanged;
  AlertRaised: AlertRaised;
};
