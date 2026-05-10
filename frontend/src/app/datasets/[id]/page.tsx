import { datasetsApi, type Dataset } from "@/api/datasets";
import { ApiError } from "@/api/client";
import { ErrorState } from "@/components/ui/empty-state";
import { DatasetDetail } from "./dataset-detail";
import Link from "next/link";

export const dynamic = "force-dynamic";

async function loadDataset(id: string): Promise<{ dataset: Dataset } | { notFound: true }> {
  try {
    return { dataset: await datasetsApi.get(id) };
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return { notFound: true };
    throw err;
  }
}

export default async function DatasetDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const result = await loadDataset(id);

  if ("notFound" in result) {
    return (
      <div className="space-y-3">
        <ErrorState message="Dataset not found." />
        <Link href="/datasets" className="text-sm underline">Back to datasets</Link>
      </div>
    );
  }

  return <DatasetDetail id={id} initialData={result.dataset} />;
}
