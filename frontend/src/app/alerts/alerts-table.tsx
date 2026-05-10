"use client";
import * as React from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { alertsApi, type AlertEventDto } from "@/api/alerts";
import type { Pipeline } from "@/api/pipelines";
import { Select } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, THead, TR, TH, TBody, TD } from "@/components/ui/table";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/empty-state";
import { formatDate } from "@/lib/utils";

function severityVariant(sev: string): "danger" | "warning" | "info" {
  switch (sev.toLowerCase()) {
    case "error": return "danger";
    case "warning": return "warning";
    default: return "info";
  }
}

export function AlertsTable({ initialData, pipelines }: { initialData: AlertEventDto[]; pipelines: Pipeline[] }) {
  const qc = useQueryClient();
  const [pipelineId, setPipelineId] = React.useState("");
  const [severity, setSeverity] = React.useState("");
  const [acknowledged, setAcknowledged] = React.useState<"" | "false" | "true">("");

  const queryKey = ["alerts", pipelineId, severity, acknowledged];
  const { data, isLoading, isError, error } = useQuery({
    queryKey,
    queryFn: () => alertsApi.listEvents({
      pipelineId: pipelineId || undefined,
      severity: severity || undefined,
      acknowledged: acknowledged === "" ? undefined : acknowledged === "true",
    }),
    initialData: (pipelineId === "" && severity === "" && acknowledged === "") ? initialData : undefined,
  });

  const ack = useMutation({
    mutationFn: (id: string) => alertsApi.acknowledge(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["alerts"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  const pipelineName = React.useMemo(() => {
    const map = new Map(pipelines.map((p) => [p.id, p.name]));
    return (id: string) => map.get(id) ?? id;
  }, [pipelines]);

  return (
    <div className="space-y-4">
      <div className="flex gap-4 items-center flex-wrap">
        <Select value={pipelineId} onChange={(e) => setPipelineId(e.target.value)}>
          <option value="">All Pipelines</option>
          {pipelines.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
        </Select>
        <Select value={severity} onChange={(e) => setSeverity(e.target.value)}>
          <option value="">All Severities</option>
          <option value="info">Info</option>
          <option value="warning">Warning</option>
          <option value="error">Error</option>
        </Select>
        <Select value={acknowledged} onChange={(e) => setAcknowledged(e.target.value as "" | "false" | "true")}>
          <option value="">All</option>
          <option value="false">Open</option>
          <option value="true">Acknowledged</option>
        </Select>
      </div>

      <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950">
        {isLoading ? (
          <LoadingState />
        ) : isError ? (
          <ErrorState message={(error as Error).message} />
        ) : !data || data.length === 0 ? (
          <EmptyState
            title="No alerts"
            hint="All clear! No alerts match the current filters."
          />
        ) : (
          <Table>
            <THead>
              <TR>
                <TH>Time</TH>
                <TH>Severity</TH>
                <TH>Pipeline</TH>
                <TH>Message</TH>
                <TH />
              </TR>
            </THead>
            <TBody>
              {data.map((alert: AlertEventDto) => (
                <TR key={alert.id} className={alert.acknowledgedAt ? "opacity-50" : undefined}>
                  <TD className="whitespace-nowrap">{formatDate(alert.createdAt)}</TD>
                  <TD><Badge variant={severityVariant(alert.severity)}>{alert.severity}</Badge></TD>
                  <TD><Link className="hover:underline" href={`/pipelines/${alert.pipelineId}`}>{pipelineName(alert.pipelineId)}</Link></TD>
                  <TD className="max-w-md truncate" title={alert.message}>{alert.message}</TD>
                  <TD className="text-right">
                    {!alert.acknowledgedAt && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={ack.isPending}
                        onClick={() => ack.mutate(alert.id)}
                      >
                        Acknowledge
                      </Button>
                    )}
                  </TD>
                </TR>
              ))}
            </TBody>
          </Table>
        )}
      </div>
    </div>
  );
}
