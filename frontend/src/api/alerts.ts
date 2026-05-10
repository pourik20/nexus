import { API_BASE_URL } from "@/lib/env";

export type AlertRuleDto = {
  id: string;
  pipelineId: string;
  name: string;
  expression: string;
  severity: string;
  enabled: boolean;
  createdAt: string;
};

export type AlertEventDto = {
  id: string;
  ruleId: string;
  runId: string;
  pipelineId: string;
  message: string;
  severity: string;
  createdAt: string;
  acknowledgedAt: string | null;
};

export type AlertEventDetailDto = AlertEventDto & {
  rule: AlertRuleDto;
};

export type CreateAlertRuleRequest = {
  pipelineId: string;
  name: string;
  expression: string;
  severity?: string;
  enabled: boolean;
};

export type UpdateAlertRuleRequest = {
  name?: string | null;
  expression?: string | null;
  severity?: string | null;
  enabled?: boolean | null;
};

async function json<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let problem: Record<string, unknown> = {};
    const text = await res.clone().text();
    if (text) {
      try { problem = JSON.parse(text); } catch { problem = { title: text }; }
    }
    const err = new Error((problem.title as string) ?? `HTTP ${res.status}`);
    Object.assign(err, { status: res.status, problem });
    throw err;
  }
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const alertsApi = {
  createRule: async (req: CreateAlertRuleRequest): Promise<AlertRuleDto> => {
    const res = await fetch(`${API_BASE_URL}/alert-rules`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });
    return json<AlertRuleDto>(res);
  },

  listRules: async (pipelineId?: string): Promise<AlertRuleDto[]> => {
    const params = new URLSearchParams();
    if (pipelineId) params.set("pipelineId", pipelineId);
    const res = await fetch(`${API_BASE_URL}/alert-rules?${params}`, { cache: "no-store" });
    return json<AlertRuleDto[]>(res);
  },

  updateRule: async (id: string, req: UpdateAlertRuleRequest): Promise<AlertRuleDto> => {
    const res = await fetch(`${API_BASE_URL}/alert-rules/${id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });
    return json<AlertRuleDto>(res);
  },

  deleteRule: async (id: string): Promise<void> => {
    const res = await fetch(`${API_BASE_URL}/alert-rules/${id}`, { method: "DELETE" });
    if (!res.ok && res.status !== 204) {
      throw new Error(`Failed to delete rule: ${res.status}`);
    }
  },

  listEvents: async (params?: { pipelineId?: string; severity?: string; acknowledged?: boolean; createdAfter?: string; createdBefore?: string }): Promise<AlertEventDto[]> => {
    const sp = new URLSearchParams();
    if (params?.pipelineId) sp.set("pipelineId", params.pipelineId);
    if (params?.severity) sp.set("severity", params.severity);
    if (params?.acknowledged !== undefined) sp.set("acknowledged", String(params.acknowledged));
    if (params?.createdAfter) sp.set("createdAfter", params.createdAfter);
    if (params?.createdBefore) sp.set("createdBefore", params.createdBefore);
    const res = await fetch(`${API_BASE_URL}/alerts?${sp}`, { cache: "no-store" });
    return json<AlertEventDto[]>(res);
  },

  getEventDetail: async (id: string): Promise<AlertEventDetailDto> => {
    const res = await fetch(`${API_BASE_URL}/alerts/${id}`, { cache: "no-store" });
    return json<AlertEventDetailDto>(res);
  },

  acknowledge: async (id: string): Promise<AlertEventDto> => {
    const res = await fetch(`${API_BASE_URL}/alerts/${id}/acknowledge`, { method: "POST" });
    return json<AlertEventDto>(res);
  },
};
