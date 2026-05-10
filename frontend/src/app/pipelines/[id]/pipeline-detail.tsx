'use client'
import * as React from 'react'
import Link from 'next/link'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import { toast } from 'sonner'
import { pipelinesApi, type Pipeline } from '@/api/pipelines'
import { runsApi, type JobRun } from '@/api/runs'
import type { Dataset } from '@/api/datasets'
import { RunStatusBadge, formatDuration } from '@/components/run-status-badge'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogClose,
} from '@/components/ui/dialog'
import { Textarea } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, THead, TR, TH, TBody, TD } from '@/components/ui/table'
import { ErrorState, LoadingState } from '@/components/ui/empty-state'
import { ApiError } from '@/api/client'
import { formatDate } from '@/lib/utils'
import { AlertRulesSection } from './alert-rules-section'

export function PipelineDetail({
  id,
  initialData,
  dataset,
}: {
  id: string
  initialData: Pipeline
  dataset: Dataset | null
}) {
  const router = useRouter()
  const qc = useQueryClient()

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['pipeline', id],
    queryFn: () => pipelinesApi.get(id),
    initialData,
  })

  const toggleActive = useMutation({
    mutationFn: (active: boolean) => pipelinesApi.update(id, { active }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pipeline', id] })
      qc.invalidateQueries({ queryKey: ['pipelines'] })
    },
    onError: (err: unknown) =>
      toast.error(
        err instanceof ApiError ?
          (err.problem.title ?? err.message)
        : (err as Error).message,
      ),
  })

  const activate = useMutation({
    mutationFn: (versionId: string) =>
      pipelinesApi.activateVersion(id, versionId),
    onSuccess: () => {
      toast.success('Version activated')
      qc.invalidateQueries({ queryKey: ['pipeline', id] })
      qc.invalidateQueries({ queryKey: ['pipelines'] })
    },
    onError: (err: unknown) =>
      toast.error(
        err instanceof ApiError ?
          (err.problem.title ?? err.message)
        : (err as Error).message,
      ),
  })

  const del = useMutation({
    mutationFn: () => pipelinesApi.remove(id),
    onSuccess: () => {
      toast.success('Pipeline deleted')
      qc.invalidateQueries({ queryKey: ['pipelines'] })
      router.push('/pipelines')
    },
    onError: (err: unknown) =>
      toast.error(
        err instanceof ApiError ?
          (err.problem.title ?? err.message)
        : (err as Error).message,
      ),
  })

  if (isLoading) return <LoadingState />
  if (isError) return <ErrorState message={(error as Error).message} />
  if (!data) return <ErrorState message='Pipeline not found' />

  const sortedVersions = [...data.versions].sort(
    (a, b) => a.version - b.version,
  )

  return (
    <div className='space-y-6'>
      <div className='flex items-start justify-between gap-4'>
        <div>
          <div className='flex items-center gap-2'>
            <h1 className='text-2xl font-semibold'>{data.name}</h1>
            {data.active ?
              <Badge variant='success'>active</Badge>
            : <Badge variant='muted'>paused</Badge>}
          </div>
          {data.description && (
            <p className='text-sm text-zinc-500 mt-1'>{data.description}</p>
          )}
          <p className='text-sm text-zinc-500 mt-1'>
            Dataset:{' '}
            <Link
              href={`/datasets/${data.datasetId}`}
              className='hover:underline'
            >
              {dataset?.name ?? data.datasetId}
            </Link>
          </p>
        </div>
        <div className='flex items-center gap-2'>
          <RunPipelineButton pipelineId={id} active={data.active} />
          <Button
            variant='outline'
            disabled={toggleActive.isPending}
            onClick={() => toggleActive.mutate(!data.active)}
          >
            {data.active ? 'Pause' : 'Resume'}
          </Button>
          <Button
            variant='destructive'
            onClick={() => del.mutate()}
            disabled={del.isPending}
          >
            {del.isPending ? 'Deleting…' : 'Delete'}
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Header</CardTitle>
        </CardHeader>
        <CardContent className='grid grid-cols-2 gap-4 text-sm'>
          <Field
            label='Schedule'
            value={
              <code className='font-mono text-xs'>{data.schedule || '—'}</code>
            }
          />
          <Field label='Active' value={data.active ? 'Yes' : 'No'} />
          <Field label='Created' value={formatDate(data.createdAt)} />
          <Field label='Updated' value={formatDate(data.updatedAt)} />
          <Field
            label='ID'
            value={<code className='font-mono text-xs'>{data.id}</code>}
          />
        </CardContent>
      </Card>

      <RecentRunsCard pipelineId={id} />

      <AlertRulesSection pipelineId={id} />

      <Card>
        <CardHeader className='flex flex-row items-center justify-between'>
          <CardTitle>Versions</CardTitle>
          <NewVersionButton pipelineId={id} />
        </CardHeader>
        <CardContent>
          <Table>
            <THead>
              <TR>
                <TH>Version</TH>
                <TH>Active</TH>
                <TH>Created</TH>
                <TH />
              </TR>
            </THead>
            <TBody>
              {sortedVersions.map((v) => (
                <TR key={v.id}>
                  <TD className='font-medium'>v{v.version}</TD>
                  <TD>
                    {v.isCurrent ?
                      <Badge variant='success'>active</Badge>
                    : <Badge variant='muted'>inactive</Badge>}
                  </TD>
                  <TD>{formatDate(v.createdAt)}</TD>
                  <TD className='text-right'>
                    {!v.isCurrent && (
                      <Button
                        size='sm'
                        variant='outline'
                        disabled={activate.isPending}
                        onClick={() => activate.mutate(v.id)}
                      >
                        Activate
                      </Button>
                    )}
                  </TD>
                </TR>
              ))}
            </TBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  )
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <div className='text-xs uppercase tracking-wide text-zinc-500'>
        {label}
      </div>
      <div className='mt-0.5'>{value}</div>
    </div>
  )
}

function RunPipelineButton({
  pipelineId,
  active,
}: {
  pipelineId: string
  active: boolean
}) {
  const qc = useQueryClient()
  const m = useMutation({
    mutationFn: () => runsApi.triggerForPipeline(pipelineId),
    onSuccess: (run) => {
      toast.success('Run started')
      qc.invalidateQueries({ queryKey: ['runs'] })
      qc.invalidateQueries({ queryKey: ['pipeline-runs', pipelineId] })
      qc.invalidateQueries({ queryKey: ['run', run.id] })
    },
    onError: (err: unknown) =>
      toast.error(
        err instanceof ApiError ?
          (err.problem.title ?? err.message)
        : (err as Error).message,
      ),
  })
  return (
    <Button onClick={() => m.mutate()} disabled={!active || m.isPending}>
      {m.isPending ? 'Starting…' : 'Run pipeline'}
    </Button>
  )
}

function RecentRunsCard({ pipelineId }: { pipelineId: string }) {
  const { data } = useQuery({
    queryKey: ['pipeline-runs', pipelineId],
    queryFn: () => runsApi.list({ pipelineId }),
  })
  const runs = (data ?? []).slice(0, 10)
  return (
    <Card>
      <CardHeader>
        <CardTitle>Recent runs</CardTitle>
      </CardHeader>
      <CardContent>
        {runs.length === 0 ?
          <p className='text-sm text-zinc-500'>No runs yet.</p>
        : <Table>
            <THead>
              <TR>
                <TH>Status</TH>
                <TH>Started</TH>
                <TH>Duration</TH>
                <TH>Records</TH>
                <TH />
              </TR>
            </THead>
            <TBody>
              {runs.map((r: JobRun) => (
                <TR key={r.id}>
                  <TD>
                    <RunStatusBadge status={r.status} />
                  </TD>
                  <TD>{formatDate(r.startedAt)}</TD>
                  <TD>{formatDuration(r.startedAt, r.finishedAt)}</TD>
                  <TD>{r.recordsProcessed}</TD>
                  <TD className='text-right'>
                    <Link
                      href={`/runs/${r.id}`}
                      className='text-sm text-zinc-500 hover:underline'
                    >
                      View →
                    </Link>
                  </TD>
                </TR>
              ))}
            </TBody>
          </Table>
        }
      </CardContent>
    </Card>
  )
}

function NewVersionButton({ pipelineId }: { pipelineId: string }) {
  const qc = useQueryClient()
  const [open, setOpen] = React.useState(false)
  const [configText, setConfigText] = React.useState('{}')
  const [stepsText, setStepsText] = React.useState('')
  const [parseError, setParseError] = React.useState<string | null>(null)

  const m = useMutation({
    mutationFn: (config: Record<string, unknown>) =>
      pipelinesApi.addVersion(pipelineId, { config }),
    onSuccess: () => {
      toast.success('Version added')
      qc.invalidateQueries({ queryKey: ['pipeline', pipelineId] })
      qc.invalidateQueries({ queryKey: ['pipelines'] })
      setOpen(false)
      setConfigText('{}')
      setParseError(null)
    },
    onError: (err: unknown) =>
      toast.error(
        err instanceof ApiError ?
          (err.problem.title ?? err.message)
        : (err as Error).message,
      ),
  })

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size='sm'>New version</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New pipeline version</DialogTitle>
        </DialogHeader>
        <form
          className='space-y-3'
          onSubmit={(e) => {
            e.preventDefault()
            setParseError(null)
            let parsed: Record<string, unknown>
            try {
              parsed = configText.trim() === '' ? {} : JSON.parse(configText)
            } catch (err) {
              setParseError((err as Error).message)
              return
            }
            // attach steps if provided
            if (stepsText.trim() !== '') {
              const stepNames = stepsText
                .split(',')
                .map((s) => s.trim())
                .filter(Boolean)
              if (stepNames.length > 0) {
                ;(parsed as Record<string, unknown>).steps = stepNames.map(
                  (name, i) => ({ name, order: i + 1 }),
                )
              }
            }
            m.mutate(parsed)
          }}
        >
          <div className='space-y-1'>
            <Label htmlFor='config'>Config (JSON)</Label>
            <Textarea
              id='config'
              className='font-mono text-xs min-h-[120px]'
              value={configText}
              onChange={(e) => setConfigText(e.target.value)}
            />
            {parseError && <p className='text-xs text-red-600'>{parseError}</p>}
            <div className='space-y-1 mt-2'>
              <Label htmlFor='steps'>Steps (comma separated)</Label>
              <input
                id='steps'
                className='w-full rounded border px-2 py-1 text-sm font-mono'
                placeholder='e.g. extract, transform, load'
                value={stepsText}
                onChange={(e) => setStepsText(e.target.value)}
              />
              <p className='text-xs text-zinc-500'>
                Optional: define ordered steps for this version.
              </p>
            </div>
            <p className='text-xs text-zinc-500'>
              The new version is added inactive. Activate it explicitly from the
              versions table.
            </p>
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type='button' variant='ghost'>
                Cancel
              </Button>
            </DialogClose>
            <Button type='submit' disabled={m.isPending}>
              {m.isPending ? 'Adding…' : 'Add version'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
