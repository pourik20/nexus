"use client";
import * as React from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { runsApi, type JobRun, type ListRunsParams, type RunStatus } from "@/api/runs";
import type { Pipeline } from "@/api/pipelines";
import { Select } from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, THead, TR, TH, TBody, TD } from "@/components/ui/table";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/empty-state";
import { RunStatusBadge, formatDuration } from "@/components/run-status-badge";
import { formatDate } from "@/lib/utils";

const statuses: RunStatus[] = ["pending", "running", "success", "failed"];

export function RunsView({ initialData, pipelines }: { initialData: JobRun[]; pipelines: Pipeline[] }) {
  const [filters, setFilters] = React.useState<ListRunsParams>({});

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["runs", filters],
    queryFn: () => runsApi.list(filters),
    initialData: Object.keys(filters).length === 0 ? initialData : undefined,
  });

  const pipelineName = React.useMemo(() => {
    const map = new Map(pipelines.map((p) => [p.id, p.name]));
    return (id: string) => map.get(id) ?? id;
  }, [pipelines]);

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
        <div className="space-y-1">
          <Label htmlFor="pipelineId">Pipeline</Label>
          <Select
            id="pipelineId"
            value={filters.pipelineId ?? ""}
            onChange={(e) => setFilters({ ...filters, pipelineId: e.target.value || undefined })}
          >
            <option value="">All</option>
            {pipelines.map((p) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </Select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="status">Status</Label>
          <Select
            id="status"
            value={filters.status ?? ""}
            onChange={(e) => setFilters({ ...filters, status: (e.target.value as RunStatus) || undefined })}
          >
            <option value="">All</option>
            {statuses.map((s) => <option key={s} value={s}>{s}</option>)}
          </Select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="startedAfter">Started after</Label>
          <Input
            id="startedAfter"
            type="datetime-local"
            value={filters.startedAfter ?? ""}
            onChange={(e) => setFilters({ ...filters, startedAfter: e.target.value || undefined })}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="startedBefore">Started before</Label>
          <Input
            id="startedBefore"
            type="datetime-local"
            value={filters.startedBefore ?? ""}
            onChange={(e) => setFilters({ ...filters, startedBefore: e.target.value || undefined })}
          />
        </div>
      </div>

      {isLoading && <LoadingState />}
      {isError && <ErrorState message={(error as Error).message} />}
      {!isLoading && !isError && (!data || data.length === 0) && (
        <EmptyState title="No runs match these filters" />
      )}
      {!isLoading && !isError && data && data.length > 0 && (
        <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950">
          <Table>
            <THead>
              <TR>
                <TH>Pipeline</TH>
                <TH>Version</TH>
                <TH>Status</TH>
                <TH>Started</TH>
                <TH>Duration</TH>
                <TH>Records</TH>
                <TH />
              </TR>
            </THead>
            <TBody>
              {data.map((r) => (
                <TR key={r.id}>
                  <TD>
                    <Link className="font-medium hover:underline" href={`/pipelines/${r.pipelineId}`}>
                      {pipelineName(r.pipelineId)}
                    </Link>
                  </TD>
                  <TD className="font-mono text-xs">v{r.pipelineVersion}</TD>
                  <TD><RunStatusBadge status={r.status} /></TD>
                  <TD>{formatDate(r.startedAt)}</TD>
                  <TD>{formatDuration(r.startedAt, r.finishedAt)}</TD>
                  <TD>{r.recordsProcessed}</TD>
                  <TD className="text-right">
                    <Link href={`/runs/${r.id}`} className="text-sm text-zinc-500 hover:underline">View →</Link>
                  </TD>
                </TR>
              ))}
            </TBody>
          </Table>
        </div>
      )}
    </div>
  );
}
