import { runsApi, type JobRun } from "@/api/runs";
import { pipelinesApi, type Pipeline } from "@/api/pipelines";
import { ErrorState } from "@/components/ui/empty-state";
import { RunsView } from "./runs-view";

export const dynamic = "force-dynamic";

export default async function RunsPage() {
  let initialRuns: JobRun[] = [];
  let pipelines: Pipeline[] = [];
  let loadError: string | null = null;
  try {
    [initialRuns, pipelines] = await Promise.all([
      runsApi.list(),
      pipelinesApi.list(),
    ]);
  } catch (err) {
    loadError = (err as Error).message;
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold">Runs</h1>
        <p className="text-sm text-zinc-500">Pipeline executions, live.</p>
      </div>
      {loadError ? (
        <ErrorState message={`Couldn't load runs: ${loadError}`} />
      ) : (
        <RunsView initialData={initialRuns} pipelines={pipelines} />
      )}
    </div>
  );
}
