import { alertsApi, type AlertEventDto } from "@/api/alerts";
import { pipelinesApi, type Pipeline } from "@/api/pipelines";
import { ErrorState } from "@/components/ui/empty-state";
import { AlertsTable } from "./alerts-table";

export const dynamic = "force-dynamic";

export default async function AlertsPage() {
  let initialEvents: AlertEventDto[] = [];
  let initialPipelines: Pipeline[] = [];
  let loadError: string | null = null;
  
  try {
    [initialEvents, initialPipelines] = await Promise.all([
      alertsApi.listEvents(),
      pipelinesApi.list(),
    ]);
  } catch (err) {
    loadError = (err as Error).message;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Alerts</h1>
          <p className="text-sm text-zinc-500">Pipeline execution alerts and anomalies.</p>
        </div>
      </div>
      {loadError ? (
        <ErrorState message={`Couldn't load alerts: ${loadError}`} />
      ) : (
        <AlertsTable initialData={initialEvents} pipelines={initialPipelines} />
      )}
    </div>
  );
}
