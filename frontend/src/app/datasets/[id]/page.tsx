import { datasetsApi } from "@/api/datasets";
import { ApiError } from "@/api/client";
import { ErrorState } from "@/components/ui/empty-state";
import { DatasetDetail } from "./dataset-detail";
import Link from "next/link";

export const dynamic = "force-dynamic";

export default async function DatasetDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  try {
    const d = await datasetsApi.get(id);
    return <DatasetDetail id={id} initialData={d} />;
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      return (
        <div className="space-y-3">
          <ErrorState message="Dataset not found." />
          <Link href="/datasets" className="text-sm underline">Back to datasets</Link>
        </div>
      );
    }
    throw err;
  }
}
