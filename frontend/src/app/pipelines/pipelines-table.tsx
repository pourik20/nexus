"use client";
import * as React from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { pipelinesApi, type CreatePipelineRequest, type Pipeline } from "@/api/pipelines";
import type { Dataset } from "@/api/datasets";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger, DialogClose } from "@/components/ui/dialog";
import { Input, Textarea } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Table, THead, TR, TH, TBody, TD } from "@/components/ui/table";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/empty-state";
import { ApiError } from "@/api/client";
import { formatDate } from "@/lib/utils";

export function PipelinesTable({ initialData, datasets }: { initialData: Pipeline[]; datasets: Dataset[] }) {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["pipelines"],
    queryFn: () => pipelinesApi.list(),
    initialData,
  });

  const datasetName = React.useMemo(() => {
    const map = new Map(datasets.map((d) => [d.id, d.name]));
    return (id: string) => map.get(id) ?? id;
  }, [datasets]);

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState message={(error as Error).message} />;
  if (!data || data.length === 0) {
    return (
      <EmptyState
        title="No pipelines yet"
        hint="Click “Create pipeline” above to add the first one."
      />
    );
  }

  return (
    <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950">
      <Table>
        <THead>
          <TR>
            <TH>Name</TH>
            <TH>Dataset</TH>
            <TH>Schedule</TH>
            <TH>Active</TH>
            <TH>Created</TH>
            <TH />
          </TR>
        </THead>
        <TBody>
          {data.map((p) => (
            <TR key={p.id}>
              <TD>
                <Link className="font-medium hover:underline" href={`/pipelines/${p.id}`}>{p.name}</Link>
                {p.description && <p className="text-xs text-zinc-500 mt-0.5 truncate max-w-md">{p.description}</p>}
              </TD>
              <TD>
                <Link className="hover:underline" href={`/datasets/${p.datasetId}`}>{datasetName(p.datasetId)}</Link>
              </TD>
              <TD className="font-mono text-xs">{p.schedule || "—"}</TD>
              <TD>{p.active ? <Badge variant="success">active</Badge> : <Badge variant="muted">paused</Badge>}</TD>
              <TD>{formatDate(p.createdAt)}</TD>
              <TD className="text-right"><Link href={`/pipelines/${p.id}`} className="text-sm text-zinc-500 hover:underline">View →</Link></TD>
            </TR>
          ))}
        </TBody>
      </Table>
    </div>
  );
}

export function CreatePipelineButton({ datasets }: { datasets: Dataset[] }) {
  const qc = useQueryClient();
  const [open, setOpen] = React.useState(false);
  const [form, setForm] = React.useState<CreatePipelineRequest>({
    datasetId: datasets[0]?.id ?? "",
    name: "",
    description: "",
    schedule: "",
    active: true,
  });
  const [errors, setErrors] = React.useState<Record<string, string[]>>({});

  const m = useMutation({
    mutationFn: (req: CreatePipelineRequest) => pipelinesApi.create(req),
    onSuccess: (p) => {
      toast.success(`Pipeline “${p.name}” created`);
      qc.invalidateQueries({ queryKey: ["pipelines"] });
      setOpen(false);
      setForm({ datasetId: datasets[0]?.id ?? "", name: "", description: "", schedule: "", active: true });
      setErrors({});
    },
    onError: (err: unknown) => {
      if (err instanceof ApiError && err.status === 400) {
        setErrors(err.fieldErrors());
        return;
      }
      if (err instanceof ApiError && err.status === 422) {
        setErrors(err.fieldErrors());
        toast.error(err.problem.title ?? err.message);
        return;
      }
      toast.error(err instanceof ApiError ? err.problem.title ?? err.message : (err as Error).message);
    },
  });

  function field(name: string) {
    return errors[name] ?? errors[name.charAt(0).toUpperCase() + name.slice(1)] ?? [];
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button disabled={datasets.length === 0}>Create pipeline</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New pipeline</DialogTitle>
        </DialogHeader>
        <form
          className="space-y-3"
          onSubmit={(e) => {
            e.preventDefault();
            setErrors({});
            m.mutate(form);
          }}
        >
          <div className="space-y-1">
            <Label htmlFor="datasetId">Dataset</Label>
            <Select
              id="datasetId"
              value={form.datasetId}
              onChange={(e) => setForm({ ...form, datasetId: e.target.value })}
              required
            >
              {datasets.map((d) => (
                <option key={d.id} value={d.id}>{d.name}</option>
              ))}
            </Select>
            {field("datasetId").map((msg, i) => <p key={i} className="text-xs text-red-600">{msg}</p>)}
          </div>
          <div className="space-y-1">
            <Label htmlFor="name">Name</Label>
            <Input id="name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            {field("name").map((msg, i) => <p key={i} className="text-xs text-red-600">{msg}</p>)}
          </div>
          <div className="space-y-1">
            <Label htmlFor="schedule">Schedule</Label>
            <Input
              id="schedule"
              placeholder="e.g. 0 2 * * *"
              value={form.schedule ?? ""}
              onChange={(e) => setForm({ ...form, schedule: e.target.value })}
            />
            {field("schedule").map((msg, i) => <p key={i} className="text-xs text-red-600">{msg}</p>)}
          </div>
          <div className="space-y-1">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={form.description ?? ""}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              id="active"
              type="checkbox"
              checked={form.active ?? true}
              onChange={(e) => setForm({ ...form, active: e.target.checked })}
            />
            <Label htmlFor="active">Active</Label>
          </div>
          <DialogFooter>
            <DialogClose asChild><Button type="button" variant="ghost">Cancel</Button></DialogClose>
            <Button type="submit" disabled={m.isPending}>{m.isPending ? "Creating…" : "Create"}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
