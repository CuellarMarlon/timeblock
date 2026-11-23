import { useCallback, useState } from "react";

export type MutationState<T> =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; error: unknown }
  | { status: "success"; data: T };

export type MutationOutcome<T> = { ok: true; data: T } | { ok: false };

export interface MutationResult<A extends unknown[], T> {
  state: MutationState<T>;
  // run nunca lanza: devuelve si salió bien y, en ese caso, los datos
  run: (...args: A) => Promise<MutationOutcome<T>>;
  // Vuelve al estado inicial, por ejemplo para quitar un aviso
  reset: () => void;
}

// Hook para operaciones de escritura (POST y PUT). Lleva el estado del envío
// (enviando, éxito o error) para que el formulario pueda bloquear el botón mientras carga
export function useApiMutation<A extends unknown[], T>(
  action: (...args: A) => Promise<T>,
): MutationResult<A, T> {
  const [state, setState] = useState<MutationState<T>>({ status: "idle" });

  const run = useCallback(
    async (...args: A): Promise<MutationOutcome<T>> => {
      setState({ status: "loading" });
      try {
        const data = await action(...args);
        setState({ status: "success", data });
        return { ok: true, data };
      } catch (error) {
        setState({ status: "error", error });
        return { ok: false };
      }
    },
    [action],
  );

  const reset = useCallback(() => setState({ status: "idle" }), []);

  return { state, run, reset };
}
