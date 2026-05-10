# Nexus — Pipeline Control Plane

Monorepo for the MSWA semester project: a control plane for monitoring and orchestrating data pipelines.

- **Backend:** .NET 10 Minimal API (`backend/Nexus.Api`)
- **Frontend:** Next.js 16 App Router (`frontend/`)
- **Database:** MongoDB
- **Realtime:** SignalR

## Prerequisites

- .NET 10 SDK
- pnpm + Node.js 20.9+
- Docker (for MongoDB)

## Running the stack

```bash
# 1. Start MongoDB
docker compose up -d

# 2. Start the API on http://localhost:5000
dotnet run --project backend/Nexus.Api

# 3. (optional) Seed demo data — see slice #7
dotnet run --project backend/Nexus.Api -- --seed

# 4. In another terminal, start the frontend on http://localhost:3000
cd frontend
pnpm install
pnpm gen:api          # regenerate src/api/types.ts from the live OpenAPI doc
pnpm dev
```

OpenAPI document: `http://localhost:5000/openapi/v1.json`.

## Demo flow

1. Open http://localhost:3000 — the dashboard shows live counters.
2. Create a dataset on `/datasets`, then a pipeline on `/pipelines` referencing it.
3. Click **Run pipeline** on the pipeline detail page. The run appears live on `/runs` and walks through `extract → transform → load` driven by SignalR.
4. Add an alert rule on the pipeline (`runtime > 5` for `RuntimeExceeds`). Trigger more runs — failed/long ones appear live on `/alerts` and bump the navbar badge.

## Architecture in three sentences

The backend is a Vertical-Slice Minimal API on MongoDB; each feature folder owns its endpoint, service, validator, and DTOs end-to-end. Pipeline runs are simulated by `RunSimulator` driving `RunStateService`, which is the only writer of run/step state and emits `RunStateChanged` / `StepStateChanged` over SignalR. The frontend is a Next.js App Router shell that fetches initial state server-side, hydrates into TanStack Query, and invalidates cache keys in response to live SignalR events — no polling.
