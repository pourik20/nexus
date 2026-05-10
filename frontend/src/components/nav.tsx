"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

const items = [
  { href: "/", label: "Dashboard" },
  { href: "/datasets", label: "Datasets" },
  { href: "/pipelines", label: "Pipelines" },
  { href: "/runs", label: "Runs" },
  { href: "/alerts", label: "Alerts" },
];

export function Nav({ openAlerts = 0 }: { openAlerts?: number }) {
  const pathname = usePathname();
  return (
    <header className="border-b border-zinc-200 dark:border-zinc-800 bg-white/80 dark:bg-zinc-950/80 backdrop-blur sticky top-0 z-30">
      <div className="mx-auto max-w-6xl px-4 h-14 flex items-center gap-6">
        <Link href="/" className="font-semibold">Nexus</Link>
        <nav className="flex items-center gap-4 flex-1">
          {items.map((it) => {
            const active = pathname === it.href || (it.href !== "/" && pathname?.startsWith(it.href));
            return (
              <Link
                key={it.href}
                href={it.href}
                className={cn(
                  "text-sm transition-colors",
                  active ? "text-zinc-900 dark:text-zinc-100 font-medium" : "text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-200",
                )}
              >
                <span className="flex items-center gap-1">
                  {it.label}
                  {it.label === "Alerts" && openAlerts > 0 && (
                    <span className="inline-flex items-center justify-center min-w-5 h-5 px-1 text-xs rounded-full bg-red-600 text-white">
                      {openAlerts}
                    </span>
                  )}
                </span>
              </Link>
            );
          })}
        </nav>
        <span className="text-xs text-zinc-500">Admin</span>
      </div>
    </header>
  );
}
