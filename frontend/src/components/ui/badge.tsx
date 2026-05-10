import * as React from "react";
import { cn } from "@/lib/utils";

type Variant = "default" | "success" | "warning" | "danger" | "info" | "muted";

const styles: Record<Variant, string> = {
  default: "bg-zinc-100 text-zinc-900 dark:bg-zinc-800 dark:text-zinc-100",
  success: "bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200",
  warning: "bg-yellow-100 text-yellow-900 dark:bg-yellow-900/40 dark:text-yellow-200",
  danger: "bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200",
  info: "bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200",
  muted: "bg-zinc-50 text-zinc-500 dark:bg-zinc-900 dark:text-zinc-400",
};

export function Badge({
  variant = "default",
  className,
  ...props
}: React.HTMLAttributes<HTMLSpanElement> & { variant?: Variant }) {
  return (
    <span className={cn("inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium", styles[variant], className)} {...props} />
  );
}
