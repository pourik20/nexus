import { API_BASE_URL } from "@/lib/env";

export type DashboardSummary = {
  totalDatasets: number;
  totalPipelines: number;
  activePipelines: number;
  runs24h: number;
  failedRuns24h: number;
  currentlyRunning: number;
  openAlerts: number;
  latestRuns: { id: string; pipelineId: string; pipelineName: string; status: string; startedAt: string; finishedAt?: string | null }[];
  latestAlerts: { id: string; ruleName: string; pipelineId: string; pipelineName: string; severity: string; message: string; createdAt: string }[];
};

export const dashboardApi = {
  summary: async (init?: RequestInit): Promise<DashboardSummary> => {
    const res = await fetch(`${API_BASE_URL}/dashboard/summary`, { cache: "no-store", ...init });
    if (!res.ok) {
      throw new Error(`Failed to load dashboard: ${res.status}`);
    }
    return res.json();
  },
};
