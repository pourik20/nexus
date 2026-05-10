import createClient, { type Middleware } from "openapi-fetch";
import type { paths } from "./types";
import { API_BASE_URL } from "@/lib/env";

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  problem: ProblemDetails;
  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? `HTTP ${status}`);
    this.status = status;
    this.problem = problem;
  }
  fieldErrors(): Record<string, string[]> {
    return this.problem.errors ?? {};
  }
}

const errorMiddleware: Middleware = {
  async onResponse({ response }) {
    if (response.ok) return;
    let problem: ProblemDetails = {};
    const text = await response.clone().text();
    if (text) {
      try {
        problem = JSON.parse(text) as ProblemDetails;
      } catch {
        problem = { title: text };
      }
    }
    throw new ApiError(response.status, problem);
  },
};

export const api = createClient<paths>({ baseUrl: API_BASE_URL });
api.use(errorMiddleware);
