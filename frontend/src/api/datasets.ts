import { API_BASE_URL } from "@/lib/env";
import { ApiError, type ProblemDetails } from "./client";

export type Dataset = {
  id: string;
  name: string;
  description?: string | null;
  owner: string;
  schemaVersion: number;
  createdAt: string;
  updatedAt: string;
};

export type CreateDatasetRequest = {
  name: string;
  description?: string;
  owner: string;
  schemaVersion: number;
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

export const datasetsApi = {
  list: async (init?: RequestInit): Promise<Dataset[]> =>
    handle(await fetch(`${API_BASE_URL}/datasets`, { cache: "no-store", ...init })),

  get: async (id: string, init?: RequestInit): Promise<Dataset> =>
    handle(await fetch(`${API_BASE_URL}/datasets/${id}`, { cache: "no-store", ...init })),

  create: async (body: CreateDatasetRequest): Promise<Dataset> =>
    handle(await fetch(`${API_BASE_URL}/datasets`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(body),
    })),

  remove: async (id: string): Promise<void> =>
    handle(await fetch(`${API_BASE_URL}/datasets/${id}`, { method: "DELETE" })),
};
