import { ApiError } from "./errors";
import type { ProblemDetails, QueryParams } from "./types";

/** URL usada si no se define `VITE_API_URL` (perfil http de Timeblock.Api). */
export const DEFAULT_API_URL = "http://localhost:5131";

export interface RequestOptions {
  query?: QueryParams;
  signal?: AbortSignal;
}

export interface ApiClient {
  get<T>(path: string, options?: RequestOptions): Promise<T>;
  post<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T>;
  put<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T>;
  patch<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T>;
}

export interface ApiClientConfig {
  baseUrl: string;
  /** Permite sustituir `fetch` en las pruebas. */
  fetch?: typeof fetch;
}

/**
 * Crea un cliente HTTP para la API. Las rutas se escriben con el prefijo
 * `/api`, por ejemplo `client.get("/api/personas")`.
 *
 * Las respuestas `204` o sin cuerpo se resuelven como `undefined`; las de
 * error lanzan {@link ApiError} con el Problem Details recibido.
 */
export function createApiClient({ baseUrl, fetch: fetchFn }: ApiClientConfig): ApiClient {
  const base = baseUrl.replace(/\/+$/, "");

  async function request<T>(
    method: string,
    path: string,
    body: unknown,
    options: RequestOptions = {},
  ): Promise<T> {
    const headers: Record<string, string> = { Accept: "application/json" };
    if (body !== undefined) headers["Content-Type"] = "application/json";

    let response: Response;
    try {
      response = await (fetchFn ?? fetch)(buildUrl(base, path, options.query), {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
        signal: options.signal,
      });
    } catch (error) {
      // Una cancelación no es un error de la API: se propaga tal cual.
      if ((error as { name?: unknown })?.name === "AbortError") throw error;
      throw new ApiError(0, undefined, { cause: error });
    }

    const data = await readBody(response);

    if (!response.ok) {
      throw new ApiError(response.status, toProblemDetails(data, response.status));
    }

    return data as T;
  }

  return {
    get: (path, options) => request("GET", path, undefined, options),
    post: (path, body, options) => request("POST", path, body, options),
    put: (path, body, options) => request("PUT", path, body, options),
    patch: (path, body, options) => request("PATCH", path, body, options),
  };
}

function buildUrl(base: string, path: string, query?: QueryParams): string {
  const url = `${base}${path.startsWith("/") ? path : `/${path}`}`;
  if (!query) return url;

  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null) params.append(key, String(value));
  }

  const search = params.toString();
  return search ? `${url}?${search}` : url;
}

async function readBody(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined;

  const text = await response.text();
  if (!text) return undefined;

  const contentType = response.headers.get("Content-Type") ?? "";
  if (!contentType.includes("json")) return text;

  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function toProblemDetails(data: unknown, status: number): ProblemDetails | undefined {
  if (typeof data === "object" && data !== null) {
    return { status, ...(data as ProblemDetails) };
  }
  return undefined;
}

/** Cliente compartido, configurado con `VITE_API_URL`. */
export const apiClient = createApiClient({
  baseUrl: import.meta.env.VITE_API_URL || DEFAULT_API_URL,
});
