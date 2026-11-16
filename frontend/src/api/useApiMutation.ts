import { useCallback, useState } from "react";

export type MutationState<T> =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; error: unknown }
  | { status: "success"; data: T };

export type MutationOutcome<T> = { ok: true; data: T } | { ok: false };

export interface MutationResult<A extends unknown[], T> {
  state: MutationState<T>;
  /** Ejecuta la operación. Nunca lanza: devuelve si tuvo éxito y los datos. */
  run: (...args: A) => Promise<MutationOutcome<T>>;
  /** Vuelve al estado inicial, por ejemplo para ocultar un aviso. */
  reset: () => void;
}

/**
 * Ejecuta una operación de escritura (POST/PUT) y expone su estado de envío,
 * éxito o error. Evita que el formulario envíe dos veces mientras `loading`.
 *
 * @example
 * const crear = useApiMutation(crearBloque);
 * const resultado = await crear.run(datos);
 * if (resultado.ok) navigate(`/bloques/${resultado.data.id}`);
 */
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
