import { API_BASE_URL } from '@/lib/env'
import { ApiError, type ProblemDetails } from './client'

export type RunStatus = 'pending' | 'running' | 'success' | 'failed'

export type JobRunStep = {
  id: string
  runId: string
  name: 'extract' | 'transform' | 'load'
  order: number
  status: RunStatus
  startedAt?: string | null
  finishedAt?: string | null
  recordsProcessed?: number
}

export type JobRun = {
  id: string
  pipelineId: string
  pipelineVersion: number
  pipelineVersionConfigSnapshot: Record<string, unknown>
  status: RunStatus
  startedAt: string
  finishedAt?: string | null
  recordsProcessed: number
  errorMessage?: string | null
  createdAt: string
  steps?: JobRunStep[] | null
}

export type ListRunsParams = {
  pipelineId?: string
  status?: RunStatus
  startedAfter?: string
  startedBefore?: string
}

export type PatchRunRequest = {
  status: 'success' | 'failed'
  errorMessage?: string
}

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let problem: ProblemDetails = {}
    const text = await res.text()
    if (text) {
      try {
        problem = JSON.parse(text)
      } catch {
        problem = { title: text }
      }
    }
    throw new ApiError(res.status, problem)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

function qs(params?: ListRunsParams): string {
  if (!params) return ''
  const parts: string[] = []
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === '') continue
    parts.push(`${encodeURIComponent(k)}=${encodeURIComponent(v as string)}`)
  }
  return parts.length ? `?${parts.join('&')}` : ''
}

export const runsApi = {
  list: async (
    params?: ListRunsParams,
    init?: RequestInit,
  ): Promise<JobRun[]> =>
    handle(
      await fetch(`${API_BASE_URL}/runs${qs(params)}`, {
        cache: 'no-store',
        ...init,
      }),
    ),

  get: async (id: string, init?: RequestInit): Promise<JobRun> =>
    handle(
      await fetch(`${API_BASE_URL}/runs/${id}`, { cache: 'no-store', ...init }),
    ),

  triggerForPipeline: async (pipelineId: string): Promise<JobRun> =>
    handle(
      await fetch(`${API_BASE_URL}/pipelines/${pipelineId}/run`, {
        method: 'POST',
      }),
    ),

  patch: async (id: string, body: PatchRunRequest): Promise<JobRun> =>
    handle(
      await fetch(`${API_BASE_URL}/runs/${id}`, {
        method: 'PATCH',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(body),
      }),
    ),
}
