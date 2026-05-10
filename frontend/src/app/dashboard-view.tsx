"use client";
import * as React from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { dashboardApi, type DashboardSummary } from "@/api/dashboard";
import { RunStatusBadge, formatDuration } from "@/components/run-status-badge";
import { Badge } from "@/components/ui/badge";
import { formatDate } from "@/lib/utils";

function StatCard({
  label,
  value,
  sub,
  href,
}: {
  label: string;
  value: number;
  sub?: string;
  href?: string;
}) {
  const content = (
    <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-5 flex flex-col gap-1 hover:shadow-sm transition-shadow">
      <p className="text-xs font-medium text-zinc-500 uppercase tracking-wide">{label}</p>
      <p className="text-3xl font-bold tabular-nums">{value}</p>
      {sub && <p className="text-xs text-zinc-400">{sub}</p>}
    </div>
  );
  return href ? <Link href={href}>{content}</Link> : content;
}

function severityVariant(sev: string): "danger" | "warning" | "info" {
  switch (sev.toLowerCase()) {
    case "error": return "danger";
    case "warning": return "warning";
    default: return "info";
  }
}

export function DashboardView({ initialData }: { initialData: DashboardSummary }) {
  const { data, isError } = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => dashboardApi.summary(),
    initialData,
    refetchInterval: 30_000,
  });

  const d = data ?? initialData;

  return (
    <div className="space-y-8">
      {isError && (
        <div className="rounded-lg bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-900 px-4 py-3 text-sm text-red-700 dark:text-red-300">
          Failed to refresh dashboard data. Showing cached values.
        </div>
      )}

      {/* Counters */}
      <section>
        <h2 className="text-sm font-semibold text-zinc-500 uppercase tracking-wide mb-3">Overview</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          <StatCard label="Datasets" value={d.totalDatasets} href="/datasets" />
          <StatCard
            label="Pipelines"
            value={d.totalPipelines}
            sub={`${d.activePipelines} active`}
            href="/pipelines"
          />
          <StatCard label="Runs (24h)" value={d.runs24h} href="/runs" />
          <StatCard label="Failed (24h)" value={d.failedRuns24h} href="/runs" />
          <StatCard label="Running now" value={d.currentlyRunning} />
          <StatCard label="Open alerts" value={d.openAlerts} href="/alerts" />
        </div>
      </section>

      {/* Latest runs + latest alerts side by side */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Latest runs */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-zinc-500 uppercase tracking-wide">Latest runs</h2>
            <Link href="/runs" className="text-xs text-zinc-400 hover:text-zinc-700 dark:hover:text-zinc-200">
              All runs →
            </Link>
          </div>
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 divide-y divide-zinc-100 dark:divide-zinc-800">
            {d.latestRuns.length === 0 ? (
              <p className="px-4 py-6 text-sm text-zinc-400 text-center">
                No runs yet. Trigger one from a pipeline detail page.
              </p>
            ) : (
              d.latestRuns.map((r) => (
                <div key={r.id} className="px-4 py-3 flex items-center gap-3">
                  <div className="flex-1 min-w-0">
                    <Link
                      href={`/pipelines/${r.pipelineId}`}
                      className="text-sm font-medium hover:underline truncate block"
                    >
                      {r.pipelineName}
                    </Link>
                    <p className="text-xs text-zinc-400">
                      {formatDate(r.startedAt)} · {formatDuration(r.startedAt, r.finishedAt ?? undefined)}
                    </p>
                  </div>
                  <RunStatusBadge status={r.status} />
                </div>
              ))
            )}
          </div>
        </section>

        {/* Latest alerts */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-zinc-500 uppercase tracking-wide">Latest alerts</h2>
            <Link href="/alerts" className="text-xs text-zinc-400 hover:text-zinc-700 dark:hover:text-zinc-200">
              All alerts →
            </Link>
          </div>
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 divide-y divide-zinc-100 dark:divide-zinc-800">
            {d.latestAlerts.length === 0 ? (
              <p className="px-4 py-6 text-sm text-zinc-400 text-center">
                No alerts — all pipelines are healthy.
              </p>
            ) : (
              d.latestAlerts.map((a) => (
                <div key={a.id} className="px-4 py-3 flex items-start gap-3">
                  <div className="pt-0.5">
                    <Badge variant={severityVariant(a.severity)}>{a.severity}</Badge>
                  </div>
                  <div className="flex-1 min-w-0">
                    <Link
                      href={`/pipelines/${a.pipelineId}`}
                      className="text-sm font-medium hover:underline truncate block"
                    >
                      {a.pipelineName}
                    </Link>
                    <p className="text-xs text-zinc-500 truncate">{a.message}</p>
                    <p className="text-xs text-zinc-400">{formatDate(a.createdAt)}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </section>
      </div>
    </div>
  );
}
