import { pipelinesApi, type Pipeline } from "@/api/pipelines";
import { datasetsApi, type Dataset } from "@/api/datasets";
import { ApiError } from "@/api/client";
import { ErrorState } from "@/components/ui/empty-state";
import { PipelineDetail } from "./pipeline-detail";
import Link from "next/link";

export const dynamic = "force-dynamic";

async function loadPipeline(id: string): Promise<{ pipeline: Pipeline; dataset: Dataset | null } | { notFound: true }> {
  let pipeline: Pipeline;
  try {
    pipeline = await pipelinesApi.get(id);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return { notFound: true };
    throw err;
  }

  let dataset: Dataset | null = null;
  try {
    dataset = await datasetsApi.get(pipeline.datasetId);
  } catch {
    dataset = null;
  }
  return { pipeline, dataset };
}

export default async function PipelineDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const result = await loadPipeline(id);

  if ("notFound" in result) {
    return (
      <div className="space-y-3">
        <ErrorState message="Pipeline not found." />
        <Link href="/pipelines" className="text-sm underline">Back to pipelines</Link>
      </div>
    );
  }

  return <PipelineDetail id={id} initialData={result.pipeline} dataset={result.dataset} />;
}
