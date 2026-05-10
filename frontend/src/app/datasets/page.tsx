import { datasetsApi, type Dataset } from "@/api/datasets";
import { CreateDatasetButton, DatasetsTable } from "./datasets-table";
import { ErrorState } from "@/components/ui/empty-state";

export const dynamic = "force-dynamic";

export default async function DatasetsPage() {
  let initial: Dataset[] = [];
  let loadError: string | null = null;
  try {
    initial = await datasetsApi.list();
  } catch (err) {
    loadError = (err as Error).message;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Datasets</h1>
          <p className="text-sm text-zinc-500">Metadata for data sources used by pipelines.</p>
        </div>
        <CreateDatasetButton />
      </div>
      {loadError ? <ErrorState message={`Couldn't load datasets: ${loadError}`} /> : <DatasetsTable initialData={initial} />}
    </div>
  );
}
