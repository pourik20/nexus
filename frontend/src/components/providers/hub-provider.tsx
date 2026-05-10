"use client";
import * as React from "react";
import { useQueryClient } from "@tanstack/react-query";
import { HubConnectionBuilder, type HubConnection, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { API_BASE_URL } from "@/lib/env";

type ConnState = "connecting" | "connected" | "reconnecting" | "disconnected";

const HubContext = React.createContext<{ state: ConnState }>({ state: "disconnected" });

export function useHubConnection() {
  return React.useContext(HubContext);
}

const handlers: Record<string, (payload: unknown, qc: ReturnType<typeof useQueryClient>) => void> = {
  PipelineUpdated: (payload, qc) => {
    const p = payload as { pipelineId?: string };
    qc.invalidateQueries({ queryKey: ["pipelines"] });
    if (p.pipelineId) qc.invalidateQueries({ queryKey: ["pipeline", p.pipelineId] });
    qc.invalidateQueries({ queryKey: ["dashboard"] });
  },
  RunStateChanged: (payload, qc) => {
    const p = payload as { runId?: string; pipelineId?: string };
    qc.invalidateQueries({ queryKey: ["runs"] });
    if (p.runId) qc.invalidateQueries({ queryKey: ["run", p.runId] });
    if (p.pipelineId) {
      qc.invalidateQueries({ queryKey: ["pipeline", p.pipelineId] });
      qc.invalidateQueries({ queryKey: ["pipeline-runs", p.pipelineId] });
    }
    qc.invalidateQueries({ queryKey: ["pipelines"] });
    qc.invalidateQueries({ queryKey: ["dashboard"] });
  },
  StepStateChanged: (payload, qc) => {
    const p = payload as { runId?: string };
    if (p.runId) qc.invalidateQueries({ queryKey: ["run", p.runId] });
  },
  AlertRaised: (_payload, qc) => {
    qc.invalidateQueries({ queryKey: ["alerts"] });
    qc.invalidateQueries({ queryKey: ["dashboard"] });
  },
};

export function HubProvider({ children }: { children: React.ReactNode }) {
  const qc = useQueryClient();
  const [state, setState] = React.useState<ConnState>("connecting");
  const connRef = React.useRef<HubConnection | null>(null);

  React.useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/control`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connRef.current = connection;

    for (const [name, fn] of Object.entries(handlers)) {
      connection.on(name, (payload: unknown) => fn(payload, qc));
    }

    connection.onreconnecting(() => setState("reconnecting"));
    connection.onreconnected(() => {
      setState("connected");
      qc.invalidateQueries();
    });
    connection.onclose(() => setState("disconnected"));

    connection
      .start()
      .then(() => setState(connection.state === HubConnectionState.Connected ? "connected" : "disconnected"))
      .catch(() => setState("disconnected"));

    return () => {
      connection.stop().catch(() => undefined);
    };
  }, [qc]);

  return <HubContext.Provider value={{ state }}>{children}</HubContext.Provider>;
}

export function ConnectionBanner() {
  const { state } = useHubConnection();
  if (state === "connected" || state === "connecting") return null;
  const isReconnecting = state === "reconnecting";
  return (
    <div
      className={
        isReconnecting
          ? "bg-yellow-100 text-yellow-900 dark:bg-yellow-900/40 dark:text-yellow-100 px-4 py-2 text-sm text-center"
          : "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-100 px-4 py-2 text-sm text-center"
      }
    >
      {isReconnecting ? "Reconnecting…" : "Disconnected from server."}
    </div>
  );
}
