'use client'
import * as React from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  alertsApi,
  type AlertRuleDto,
  type CreateAlertRuleRequest,
  type UpdateAlertRuleRequest,
} from '@/api/alerts'
import { Button } from '@/components/ui/button'
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
import { Input, Textarea } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Table, THead, TR, TH, TBody, TD } from '@/components/ui/table'
import {
  EmptyState,
  ErrorState,
  LoadingState,
} from '@/components/ui/empty-state'

export function AlertRulesSection({ pipelineId }: { pipelineId: string }) {
  const qc = useQueryClient()
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['alert-rules', pipelineId],
    queryFn: () => alertsApi.listRules(pipelineId),
  })

  const toggleEnabled = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      alertsApi.updateRule(id, { enabled }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['alert-rules', pipelineId] })
    },
    onError: (err: unknown) =>
      toast.error(err instanceof Error ? err.message : 'Failed to update rule'),
  })

  const del = useMutation({
    mutationFn: (id: string) => alertsApi.deleteRule(id),
    onSuccess: () => {
      toast.success('Alert rule deleted')
      qc.invalidateQueries({ queryKey: ['alert-rules', pipelineId] })
    },
    onError: (err: unknown) =>
      toast.error(err instanceof Error ? err.message : 'Failed to delete rule'),
  })

  return (
    <Card>
      <CardHeader className='flex flex-row items-center justify-between'>
        <CardTitle>Alert rules</CardTitle>
        <RuleDialog pipelineId={pipelineId} />
      </CardHeader>
      <CardContent>
        {isLoading ?
          <LoadingState />
        : isError ?
          <ErrorState message={(error as Error).message} />
        : !data || data.length === 0 ?
          <EmptyState
            title='No alert rules'
            hint='Create an alert rule to monitor this pipeline.'
          />
        : <Table>
            <THead>
              <TR>
                <TH>Name</TH>
                <TH>Enabled</TH>
                <TH />
              </TR>
            </THead>
            <TBody>
              {data.map((r: AlertRuleDto) => (
                <TR key={r.id}>
                  <TD className='font-medium'>{r.name}</TD>
                  <TD>
                    {r.enabled ?
                      <Badge variant='success'>enabled</Badge>
                    : <Badge variant='muted'>disabled</Badge>}
                  </TD>
                  <TD className='text-right space-x-2'>
                    <Button
                      size='sm'
                      variant='outline'
                      disabled={toggleEnabled.isPending}
                      onClick={() =>
                        toggleEnabled.mutate({ id: r.id, enabled: !r.enabled })
                      }
                    >
                      {r.enabled ? 'Disable' : 'Enable'}
                    </Button>
                    <RuleDialog pipelineId={pipelineId} initialData={r} />
                    <Button
                      size='sm'
                      variant='destructive'
                      disabled={del.isPending}
                      onClick={() => {
                        if (
                          confirm('Are you sure you want to delete this rule?')
                        ) {
                          del.mutate(r.id)
                        }
                      }}
                    >
                      Delete
                    </Button>
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

type RuleFormState = {
  name: string
  expression: string
  enabled: boolean
}

function emptyForm(): RuleFormState {
  return { name: '', expression: '', enabled: true }
}

function formFromRule(r: AlertRuleDto): RuleFormState {
  return {
    name: r.name,
    expression: r.expression,
    enabled: r.enabled,
  }
}

function RuleDialog({
  pipelineId,
  initialData,
}: {
  pipelineId: string
  initialData?: AlertRuleDto
}) {
  const qc = useQueryClient()
  const [open, setOpen] = React.useState(false)
  const [form, setForm] = React.useState<RuleFormState>(
    initialData ? formFromRule(initialData) : emptyForm(),
  )
  const [errors, setErrors] = React.useState<Record<string, string[]>>({})

  React.useEffect(() => {
    if (open) {
      setForm(initialData ? formFromRule(initialData) : emptyForm())
      setErrors({})
    }
  }, [open, initialData])

  const m = useMutation({
    mutationFn: () => {
      if (initialData) {
        const req: UpdateAlertRuleRequest = {
          name: form.name,
          expression: form.expression,
          enabled: form.enabled,
        }
        return alertsApi.updateRule(initialData.id, req)
      }
      const req: CreateAlertRuleRequest = {
        pipelineId,
        name: form.name,
        expression: form.expression,
        enabled: form.enabled,
      }
      return alertsApi.createRule(req)
    },
    onSuccess: () => {
      toast.success(initialData ? 'Alert rule updated' : 'Alert rule created')
      qc.invalidateQueries({ queryKey: ['alert-rules', pipelineId] })
      setOpen(false)
    },
    onError: (err: unknown) => {
      const e = err as {
        status?: number
        problem?: { errors?: Record<string, string[]>; title?: string }
      }
      if (e.status === 400 && e.problem?.errors) {
        setErrors(e.problem.errors)
        return
      }
      toast.error(
        e.problem?.title ??
          (err instanceof Error ? err.message : 'An error occurred'),
      )
    },
  })

  function field(name: string) {
    return (
      errors[name] ?? errors[name.charAt(0).toUpperCase() + name.slice(1)] ?? []
    )
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size='sm' variant={initialData ? 'outline' : 'default'}>
          {initialData ? 'Edit' : 'New rule'}
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {initialData ? 'Edit alert rule' : 'New alert rule'}
          </DialogTitle>
        </DialogHeader>
        <form
          className='space-y-3'
          onSubmit={(e) => {
            e.preventDefault()
            setErrors({})
            m.mutate()
          }}
        >
          <div className='space-y-1'>
            <Label htmlFor='rule-name'>Name</Label>
            <Input
              id='rule-name'
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
            {field('name').map((msg, i) => (
              <p key={i} className='text-xs text-red-600'>
                {msg}
              </p>
            ))}
          </div>

          {/* type and runtime threshold removed from the form per UI decision */}

          <div className='space-y-1'>
            <Label htmlFor='rule-expression'>
              Expression (JSONata) —{' '}
              <a
                href='https://jsonata.org'
                target='_blank'
                rel='noreferrer'
                className='text-blue-500 hover:underline text-xs'
              >
                docs
              </a>
            </Label>
            <Textarea
              id='rule-expression'
              placeholder='e.g. runtime > 300'
              value={form.expression}
              onChange={(e) => setForm({ ...form, expression: e.target.value })}
              className='font-mono text-xs h-24'
            />
            <p className='text-xs text-zinc-500'>
              Context: runtime (seconds), status, recordsProcessed, finishedAt,
              pipeline (name, schedule, version)
            </p>
            {field('expression').map((msg, i) => (
              <p key={i} className='text-xs text-red-600'>
                {msg}
              </p>
            ))}
          </div>

          <div className='flex items-center gap-2 pt-2'>
            <input
              id='rule-enabled'
              type='checkbox'
              checked={form.enabled}
              onChange={(e) => setForm({ ...form, enabled: e.target.checked })}
            />
            <Label htmlFor='rule-enabled'>Enabled</Label>
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button type='button' variant='ghost'>
                Cancel
              </Button>
            </DialogClose>
            <Button type='submit' disabled={m.isPending}>
              {m.isPending ? 'Saving…' : 'Save rule'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
