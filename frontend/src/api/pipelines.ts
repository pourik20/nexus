import { API_BASE_URL } from "@/lib/env";
import { ApiError, type ProblemDetails } from "./client";

export type PipelineVersion = {
  id: string;
  version: number;
  config: Record<string, unknown>;
  isCurrent: boolean;
  createdAt: string;
};

export type Pipeline = {
  id: string;
  datasetId: string;
  name: string;
  description?: string | null;
  schedule?: string | null;
  active: boolean;
  versions: PipelineVersion[];
  createdAt: string;
  updatedAt: string;
  lastRunAt?: string | null;
  lastRunStatus?: string | null;
};

export type CreatePipelineRequest = {
  datasetId: string;
  name: string;
  description?: string;
  schedule?: string;
  active?: boolean;
};

export type UpdatePipelineRequest = {
  name?: string;
  description?: string;
  schedule?: string;
  active?: boolean;
};

export type AddVersionRequest = {
  config?: Record<string, unknown>;
};

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let problem: ProblemDetails = {};
    const text = await res.text();
    if (text) {
      try { problem = JSON.parse(text); } catch { problem = { title: text }; }
    }
    throw new ApiError(res.status, problem);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const pipelinesApi = {
  list: async (params?: { datasetId?: string }, init?: RequestInit): Promise<Pipeline[]> => {
    const qs = params?.datasetId ? `?datasetId=${encodeURIComponent(params.datasetId)}` : "";
    return handle(await fetch(`${API_BASE_URL}/pipelines${qs}`, { cache: "no-store", ...init }));
  },

  get: async (id: string, init?: RequestInit): Promise<Pipeline> =>
    handle(await fetch(`${API_BASE_URL}/pipelines/${id}`, { cache: "no-store", ...init })),

  create: async (body: CreatePipelineRequest): Promise<Pipeline> =>
    handle(await fetch(`${API_BASE_URL}/pipelines`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    })),

  update: async (id: string, body: UpdatePipelineRequest): Promise<Pipeline> =>
    handle(await fetch(`${API_BASE_URL}/pipelines/${id}`, {
      method: "PATCH",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    })),

  remove: async (id: string): Promise<void> =>
    handle(await fetch(`${API_BASE_URL}/pipelines/${id}`, { method: "DELETE" })),

  addVersion: async (id: string, body: AddVersionRequest): Promise<Pipeline> =>
    handle(await fetch(`${API_BASE_URL}/pipelines/${id}/versions`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    })),

  activateVersion: async (id: string, versionId: string): Promise<Pipeline> =>
    handle(await fetch(`${API_BASE_URL}/pipelines/${id}/versions/${versionId}/activate`, {
      method: "PATCH",
    })),
};
