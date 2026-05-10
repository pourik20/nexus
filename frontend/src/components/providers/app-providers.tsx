"use client";
import * as React from "react";
import { Toaster } from "sonner";
import { QueryProvider } from "./query-provider";
import { HubProvider, ConnectionBanner } from "./hub-provider";

export function AppProviders({ children }: { children: React.ReactNode }) {
  return (
    <QueryProvider>
      <HubProvider>
        <ConnectionBanner />
        {children}
        <Toaster position="top-right" richColors />
      </HubProvider>
    </QueryProvider>
  );
}
