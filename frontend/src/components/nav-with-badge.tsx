"use client";
import { useQuery } from "@tanstack/react-query";
import { Nav } from "./nav";
import { dashboardApi } from "@/api/dashboard";

export function NavWithBadge() {
  const { data } = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => dashboardApi.summary().catch(() => null),
    staleTime: 10_000,
  });
  return <Nav openAlerts={data?.openAlerts ?? 0} />;
}
