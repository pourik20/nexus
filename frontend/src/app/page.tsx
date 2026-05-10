import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export default function HomePage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Nexus</h1>
        <p className="text-sm text-zinc-500">Pipeline control plane.</p>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card><CardHeader><CardTitle>Datasets</CardTitle></CardHeader><CardContent><Link href="/datasets" className="text-sm text-blue-600 underline">Manage datasets →</Link></CardContent></Card>
        <Card><CardHeader><CardTitle>Pipelines</CardTitle></CardHeader><CardContent><Link href="/pipelines" className="text-sm text-blue-600 underline">Manage pipelines →</Link></CardContent></Card>
        <Card><CardHeader><CardTitle>Runs</CardTitle></CardHeader><CardContent><Link href="/runs" className="text-sm text-blue-600 underline">Recent runs →</Link></CardContent></Card>
      </div>
      <p className="text-xs text-zinc-500">A live dashboard lands in slice #7.</p>
    </div>
  );
}
