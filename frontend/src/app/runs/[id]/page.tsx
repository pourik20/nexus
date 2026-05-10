import Link from "next/link";
import { runsApi, type JobRun } from "@/api/runs";
import { ApiError } from "@/api/client";
import { ErrorState } from "@/components/ui/empty-state";
import { RunDetail } from "./run-detail";

export const dynamic = "force-dynamic";

export default async function RunDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  let run: JobRun | null = null;
  try {
    run = await runsApi.get(id);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      return (
        <div className="space-y-3">
          <ErrorState message="Run not found." />
          <Link href="/runs" className="text-sm underline">Back to runs</Link>
        </div>
      );
    }
    throw err;
  }
  return <RunDetail id={id} initialData={run} />;
}
