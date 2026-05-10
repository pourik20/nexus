import { dashboardApi, type DashboardSummary } from "@/api/dashboard";
import { DashboardView } from "./dashboard-view";

export const dynamic = "force-dynamic";

const EMPTY: DashboardSummary = {
  totalDatasets: 0,
  totalPipelines: 0,
  activePipelines: 0,
  runs24h: 0,
  failedRuns24h: 0,
  currentlyRunning: 0,
  openAlerts: 0,
  latestRuns: [],
  latestAlerts: [],
};

export default async function HomePage() {
  let initial = EMPTY;
  try {
    initial = await dashboardApi.summary();
  } catch {
    // Fall back to empty — client will retry on hydration
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-zinc-500">Live overview of your pipeline control plane.</p>
      </div>
      <DashboardView initialData={initial} />
    </div>
  );
}
