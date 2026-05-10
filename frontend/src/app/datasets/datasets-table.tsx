"use client";
import * as React from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { datasetsApi, type CreateDatasetRequest, type Dataset } from "@/api/datasets";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger, DialogClose } from "@/components/ui/dialog";
import { Input, Textarea } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, THead, TR, TH, TBody, TD } from "@/components/ui/table";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/empty-state";
import { ApiError } from "@/api/client";
import { formatDate } from "@/lib/utils";

export function DatasetsTable({ initialData }: { initialData: Dataset[] }) {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["datasets"],
    queryFn: () => datasetsApi.list(),
    initialData,
  });

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState message={(error as Error).message} />;
  if (!data || data.length === 0) {
    return (
      <EmptyState
        title="No datasets yet"
        hint="Click “Create dataset” above to add the first one."
      />
    );
  }

  return (
    <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950">
      <Table>
        <THead>
          <TR>
            <TH>Name</TH>
            <TH>Owner</TH>
            <TH>Schema</TH>
            <TH>Created</TH>
            <TH />
          </TR>
        </THead>
        <TBody>
          {data.map((d) => (
            <TR key={d.id}>
              <TD>
                <Link className="font-medium hover:underline" href={`/datasets/${d.id}`}>{d.name}</Link>
                {d.description && <p className="text-xs text-zinc-500 mt-0.5 truncate max-w-md">{d.description}</p>}
              </TD>
              <TD>{d.owner}</TD>
              <TD>v{d.schemaVersion}</TD>
              <TD>{formatDate(d.createdAt)}</TD>
              <TD className="text-right"><Link href={`/datasets/${d.id}`} className="text-sm text-zinc-500 hover:underline">View →</Link></TD>
            </TR>
          ))}
        </TBody>
      </Table>
    </div>
  );
}

export function CreateDatasetButton() {
  const qc = useQueryClient();
  const [open, setOpen] = React.useState(false);
  const [form, setForm] = React.useState<CreateDatasetRequest>({ name: "", description: "", owner: "", schemaVersion: 1 });
  const [errors, setErrors] = React.useState<Record<string, string[]>>({});

  const m = useMutation({
    mutationFn: (req: CreateDatasetRequest) => datasetsApi.create(req),
    onSuccess: (d) => {
      toast.success(`Dataset “${d.name}” created`);
      qc.invalidateQueries({ queryKey: ["datasets"] });
      setOpen(false);
      setForm({ name: "", description: "", owner: "", schemaVersion: 1 });
      setErrors({});
    },
    onError: (err) => {
      if (err instanceof ApiError && err.status === 400) {
        setErrors(err.fieldErrors());
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
        <Button>Create dataset</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New dataset</DialogTitle>
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
            <Label htmlFor="name">Name</Label>
            <Input id="name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            {field("name").map((m, i) => <p key={i} className="text-xs text-red-600">{m}</p>)}
          </div>
          <div className="space-y-1">
            <Label htmlFor="owner">Owner</Label>
            <Input id="owner" value={form.owner} onChange={(e) => setForm({ ...form, owner: e.target.value })} required />
            {field("owner").map((m, i) => <p key={i} className="text-xs text-red-600">{m}</p>)}
          </div>
          <div className="space-y-1">
            <Label htmlFor="schemaVersion">Schema version</Label>
            <Input id="schemaVersion" type="number" min={1} value={form.schemaVersion}
              onChange={(e) => setForm({ ...form, schemaVersion: Number(e.target.value) })} required />
            {field("schemaVersion").map((m, i) => <p key={i} className="text-xs text-red-600">{m}</p>)}
          </div>
          <div className="space-y-1">
            <Label htmlFor="description">Description</Label>
            <Textarea id="description" value={form.description ?? ""} onChange={(e) => setForm({ ...form, description: e.target.value })} />
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
