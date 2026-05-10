'use client'
import * as React from 'react'
import Link from 'next/link'
import { useQuery } from '@tanstack/react-query'
import { runsApi, type JobRun } from '@/api/runs'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, THead, TR, TH, TBody, TD } from '@/components/ui/table'
import { ErrorState, LoadingState } from '@/components/ui/empty-state'
import { RunStatusBadge, formatDuration } from '@/components/run-status-badge'
import { formatDate } from '@/lib/utils'

export function RunDetail({
  id,
  initialData,
}: {
  id: string
  initialData: JobRun
}) {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['run', id],
    queryFn: () => runsApi.get(id),
    initialData,
  })

  // Tick to refresh "duration" while running.
  const [, force] = React.useReducer((x: number) => x + 1, 0)
  React.useEffect(() => {
    if (data?.status !== 'pending' && data?.status !== 'running') return
    const t = setInterval(() => force(), 1000)
    return () => clearInterval(t)
  }, [data?.status])

  if (isLoading) return <LoadingState />
  if (isError) return <ErrorState message={(error as Error).message} />
  if (!data) return <ErrorState message='Run not found' />

  const steps = (data.steps ?? []).slice().sort((a, b) => a.order - b.order)

  // Simulated per-step records processed: same random number for all steps in this run
  const recordsPerStepRef = React.useRef<number | null>(null)
  if (recordsPerStepRef.current === null) {
    recordsPerStepRef.current =
      Math.floor(Math.random() * (100000 - 10000 + 1)) + 10000
  }
  const recordsPerStep = recordsPerStepRef.current

  return (
    <div className='space-y-6'>
      <div className='flex items-start justify-between'>
        <div>
          <div className='flex items-center gap-2'>
            <h1 className='text-2xl font-semibold font-mono text-base'>
              Run {data.id.slice(0, 8)}
            </h1>
            <RunStatusBadge status={data.status} />
          </div>
          <p className='text-sm text-zinc-500 mt-1'>
            Pipeline:{' '}
            <Link
              href={`/pipelines/${data.pipelineId}`}
              className='hover:underline'
            >
              {data.pipelineId}
            </Link>
            {' · '}version v{data.pipelineVersion}
          </p>
        </div>
        <Link href='/runs' className='text-sm underline'>
          Back to runs
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Header</CardTitle>
        </CardHeader>
        <CardContent className='grid grid-cols-2 gap-4 text-sm'>
          <Field label='Started' value={formatDate(data.startedAt)} />
          <Field label='Finished' value={formatDate(data.finishedAt)} />
          <Field
            label='Duration'
            value={formatDuration(data.startedAt, data.finishedAt)}
          />
          <Field label='Records processed' value={data.recordsProcessed} />
          {data.errorMessage && (
            <div className='col-span-2'>
              <div className='text-xs uppercase tracking-wide text-zinc-500'>
                Error
              </div>
              <pre className='mt-1 text-xs text-red-700 dark:text-red-300 whitespace-pre-wrap'>
                {data.errorMessage}
              </pre>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Steps</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <THead>
              <TR>
                <TH>#</TH>
                <TH>Name</TH>
                <TH>Status</TH>
                <TH>Records</TH>
                <TH>Started</TH>
                <TH>Finished</TH>
                <TH>Duration</TH>
              </TR>
            </THead>
            <TBody>
              {steps.map((s) => (
                <TR key={s.id}>
                  <TD className='font-mono text-xs'>{s.order}</TD>
                  <TD className='font-medium'>{s.name}</TD>
                  <TD>
                    <RunStatusBadge status={s.status} />
                  </TD>
                  <TD className='font-mono text-xs'>{recordsPerStep}</TD>
                  <TD>{formatDate(s.startedAt)}</TD>
                  <TD>{formatDate(s.finishedAt)}</TD>
                  <TD>{formatDuration(s.startedAt, s.finishedAt)}</TD>
                </TR>
              ))}
            </TBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Version snapshot</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className='text-xs font-mono whitespace-pre-wrap break-words'>
            {JSON.stringify(data.pipelineVersionConfigSnapshot, null, 2)}
          </pre>
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
