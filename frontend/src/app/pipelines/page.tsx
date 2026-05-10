import { pipelinesApi, type Pipeline } from "@/api/pipelines";
import { datasetsApi, type Dataset } from "@/api/datasets";
import { CreatePipelineButton, PipelinesTable } from "./pipelines-table";
import { ErrorState } from "@/components/ui/empty-state";

export const dynamic = "force-dynamic";

export default async function PipelinesPage() {
  let initialPipelines: Pipeline[] = [];
  let initialDatasets: Dataset[] = [];
  let loadError: string | null = null;
  try {
    [initialPipelines, initialDatasets] = await Promise.all([
      pipelinesApi.list(),
      datasetsApi.list(),
    ]);
  } catch (err) {
    loadError = (err as Error).message;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Pipelines</h1>
          <p className="text-sm text-zinc-500">Definitions that process datasets on a schedule.</p>
        </div>
        <CreatePipelineButton datasets={initialDatasets} />
      </div>
      {loadError ? (
        <ErrorState message={`Couldn't load pipelines: ${loadError}`} />
      ) : (
        <PipelinesTable initialData={initialPipelines} datasets={initialDatasets} />
      )}
    </div>
  );
}
