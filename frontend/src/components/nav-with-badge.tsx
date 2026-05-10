"use client";
import { useQuery } from "@tanstack/react-query";
import { Nav } from "./nav";
import { alertsApi } from "@/api/alerts";

export function NavWithBadge() {
  const { data } = useQuery({
    queryKey: ["alerts"],
    queryFn: () => alertsApi.listEvents().catch(() => []),
    staleTime: 10_000,
  });
  return <Nav openAlerts={data?.length ?? 0} />;
}
