"use client";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { datasetsApi, type Dataset } from "@/api/datasets";
import { pipelinesApi } from "@/api/pipelines";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, THead, TR, TH, TBody, TD } from "@/components/ui/table";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/empty-state";
import { ApiError } from "@/api/client";
import { formatDate } from "@/lib/utils";

export function DatasetDetail({ id, initialData }: { id: string; initialData: Dataset }) {
  const router = useRouter();
  const qc = useQueryClient();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["dataset", id],
    queryFn: () => datasetsApi.get(id),
    initialData,
  });

  const del = useMutation({
    mutationFn: () => datasetsApi.remove(id),
    onSuccess: () => {
      toast.success("Dataset deleted");
      qc.invalidateQueries({ queryKey: ["datasets"] });
      router.push("/datasets");
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.title ?? err.message : (err as Error).message),
  });

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState message={(error as Error).message} />;
  if (!data) return <ErrorState message="Dataset not found" />;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{data.name}</h1>
          {data.description && <p className="text-sm text-zinc-500 mt-1">{data.description}</p>}
        </div>
        <Button variant="destructive" onClick={() => del.mutate()} disabled={del.isPending}>
          {del.isPending ? "Deleting…" : "Delete"}
        </Button>
      </div>

      <Card>
        <CardHeader><CardTitle>Metadata</CardTitle></CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 text-sm">
          <Field label="Owner" value={data.owner} />
          <Field label="Schema version" value={`v${data.schemaVersion}`} />
          <Field label="Created" value={formatDate(data.createdAt)} />
          <Field label="Updated" value={formatDate(data.updatedAt)} />
          <Field label="ID" value={<code className="font-mono text-xs">{data.id}</code>} />
        </CardContent>
      </Card>

      <PipelinesUsingDataset datasetId={id} />
    </div>
  );
}

function PipelinesUsingDataset({ datasetId }: { datasetId: string }) {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["pipelines", { datasetId }],
    queryFn: () => pipelinesApi.list({ datasetId }),
  });

  return (
    <Card>
      <CardHeader><CardTitle>Pipelines using this dataset</CardTitle></CardHeader>
      <CardContent>
        {isLoading && <LoadingState />}
        {isError && <ErrorState message={(error as Error).message} />}
        {!isLoading && !isError && (!data || data.length === 0) && (
          <EmptyState title="No pipelines reference this dataset." />
        )}
        {data && data.length > 0 && (
          <Table>
            <THead>
              <TR>
                <TH>Name</TH>
                <TH>Schedule</TH>
                <TH>Active</TH>
                <TH />
              </TR>
            </THead>
            <TBody>
              {data.map((p) => (
                <TR key={p.id}>
                  <TD>
                    <Link className="font-medium hover:underline" href={`/pipelines/${p.id}`}>{p.name}</Link>
                  </TD>
                  <TD className="font-mono text-xs">{p.schedule || "—"}</TD>
                  <TD>{p.active ? <Badge variant="success">active</Badge> : <Badge variant="muted">paused</Badge>}</TD>
                  <TD className="text-right">
                    <Link href={`/pipelines/${p.id}`} className="text-sm text-zinc-500 hover:underline">View →</Link>
                  </TD>
                </TR>
              ))}
            </TBody>
          </Table>
        )}
      </CardContent>
    </Card>
  );
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <div className="text-xs uppercase tracking-wide text-zinc-500">{label}</div>
      <div className="mt-0.5">{value}</div>
    </div>
  );
}
