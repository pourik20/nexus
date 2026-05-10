# Nexus — Pipeline Control Plane

A demo control plane for orchestrating and monitoring data pipelines in an autonomous taxi fleet. Built as a school project (MSWA).

---

## Prerequisites

| Tool                    | Version |
| ----------------------- | ------- |
| .NET SDK                | 10.0+   |
| pnpm                    | 9+      |
| Docker + Docker Compose | 27+     |
| Node.js                 | 22+     |

---

## Running the full stack

### 1. Start MongoDB

```bash
docker compose up -d mongo
```

This starts MongoDB on `localhost:27017` with a persistent volume.

### 2. Start the API

```bash
cd backend/Nexus.Api
dotnet run
```

The API listens on `http://localhost:3001`.

### 3. Seed the database (taxi-fleet fixture)

In a separate terminal, while MongoDB is running:

```bash
cd backend/Nexus.Api
dotnet run -- --seed
```

This is idempotent — running it multiple times always produces the same state (drops all collections and re-inserts). After seeding you'll see output like:

```
=== Nexus Seed CLI ===
Dropping collections…
  datasets:    3
  pipelines:   4
  alertRules:  2
  jobRuns:     21
  jobRunSteps: 60
  alertEvents: 4

✓ Seed completed successfully.
```

### 4. Start the frontend

```bash
cd frontend
pnpm install
pnpm gen:api    # regenerates TypeScript types from the live OpenAPI spec
pnpm dev
```

Open [http://localhost:3000](http://localhost:3000).

---

## Docker — full stack in one command

> **Note**: `NEXT_PUBLIC_API_BASE_URL` is baked in at build time. If you change the API host, rebuild the frontend image.

```bash
docker compose up --build
```

Then seed:

```bash
docker compose exec api dotnet Nexus.Api.dll --seed
# or, if you prefer to run from the SDK image:
docker compose run --rm api dotnet run -- --seed
```

---

## Demo flow

1. Open [http://localhost:3000](http://localhost:3000) — the Dashboard shows live counters.
2. Go to **Pipelines** → open _Telemetry Ingest_ → click **Run now**.
3. Watch the **Runs** page — the new run appears with status _running_ in real time (no refresh).
4. When the run finishes, if it failed, the **Alerts** badge increments and the alert appears on the Dashboard.

---

## Architecture (3-sentence story)

Nexus uses a **push model**: when a pipeline's run changes state, the backend emits an event over an in-memory bus (MediatR) which triggers alert evaluation and pushes a SignalR message to all connected clients.

The frontend never polls — it subscribes to the SignalR hub and invalidates the relevant TanStack Query caches on each event, so the UI stays live without refresh.

The backend is a .NET 10 Minimal API backed by MongoDB; all real-time functionality runs inside the process using `System.Threading.Channels` for event batching before broadcast.

---

## Project structure

```
nexus/
├── backend/
│   └── Nexus.Api/
│       ├── Domain/          # Entities (Dataset, Pipeline, JobRun, AlertRule, AlertEvent)
│       ├── Features/        # Vertical slices: Datasets, Pipelines, Runs, Alerts, Dashboard
│       └── Infrastructure/  # Mongo, SignalR, Auth, Seed
├── frontend/
│   └── src/
│       ├── api/             # Typed fetch clients
│       ├── app/             # Next.js App Router pages
│       └── components/      # UI primitives + providers
├── docker-compose.yml
└── docs/
```
