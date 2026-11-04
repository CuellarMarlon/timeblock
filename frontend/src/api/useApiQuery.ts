import { useCallback, useEffect, useState } from "react";

export type QueryState<T> =
  { status: "loading" } | { status: "error"; error: unknown } | { status: "success"; data: T };

export type QueryResult<T> = QueryState<T> & {
  /** Repite la consulta, por ejemplo tras un error. */
  reload: () => void;
};

interface Resolved<T> {
  load: (signal: AbortSignal) => Promise<T>;
  attempt: number;
  state: QueryState<T>;
}

/**
 * Ejecuta una consulta a la API y expone su estado de carga, error o éxito.
 *
 * `load` debe ser estable (`useCallback`): cada vez que cambia se repite la
 * consulta y se cancela la anterior.
 *
 * @example
 * const load = useCallback((signal: AbortSignal) => apiClient.get<Persona[]>("/api/personas", { signal }), []);
 * const personas = useApiQuery(load);
 */
export function useApiQuery<T>(load: (signal: AbortSignal) => Promise<T>): QueryResult<T> {
  const [attempt, setAttempt] = useState(0);
  const [resolved, setResolved] = useState<Resolved<T> | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    load(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) {
          setResolved({ load, attempt, state: { status: "success", data } });
        }
      },
      (error: unknown) => {
        if (!controller.signal.aborted) {
          setResolved({ load, attempt, state: { status: "error", error } });
        }
      },
    );

    return () => controller.abort();
  }, [load, attempt]);

  const reload = useCallback(() => setAttempt((n) => n + 1), []);

  // Mientras no haya resultado para la consulta actual, se considera en carga.
  const state: QueryState<T> =
    resolved && resolved.load === load && resolved.attempt === attempt
      ? resolved.state
      : { status: "loading" };

  return { ...state, reload };
}
