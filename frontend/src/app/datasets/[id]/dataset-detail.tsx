"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { datasetsApi, type Dataset } from "@/api/datasets";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorState, LoadingState } from "@/components/ui/empty-state";
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
    </div>
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
