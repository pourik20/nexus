import { Badge } from "@/components/ui/badge";
import type { RunStatus } from "@/api/runs";

const variantByStatus: Record<RunStatus, "muted" | "info" | "success" | "danger"> = {
  pending: "muted",
  running: "info",
  success: "success",
  failed: "danger",
};

export function RunStatusBadge({ status }: { status: RunStatus | string }) {
  const v = (variantByStatus as Record<string, "muted" | "info" | "success" | "danger">)[status] ?? "muted";
  return <Badge variant={v}>{status}</Badge>;
}

export function formatDuration(startedAt?: string | null, finishedAt?: string | null): string {
  if (!startedAt) return "—";
  const start = new Date(startedAt).getTime();
  const end = finishedAt ? new Date(finishedAt).getTime() : Date.now();
  const ms = Math.max(0, end - start);
  if (ms < 1000) return `${ms} ms`;
  const s = Math.floor(ms / 1000);
  if (s < 60) return `${s}s`;
  const m = Math.floor(s / 60);
  return `${m}m ${s % 60}s`;
}
