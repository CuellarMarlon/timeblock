import { useCallback, useRef, useState } from "react";

export type MutationState<T> =
  | { status: "idle" }
  | { status: "pending" }
  | { status: "error"; error: unknown }
  | { status: "success"; data: T };

export type MutationResult<TInput, TData> = MutationState<TData> & {
  /** Ejecuta la mutación. Devuelve true si terminó con éxito. */
  submit: (input: TInput) => Promise<boolean>;
  /** Vuelve al estado inicial (*/
  reset: () => void;
};

export function useApiMutation<TInput, TData>(
  action: (input: TInput) => Promise<TData>,
): MutationResult<TInput, TData> {
  const [state, setState] = useState<MutationState<TData>>({ status: "idle" });
  const pending = useRef(false);

  const submit = useCallback(
    async (input: TInput) => {
      // Evita envíos duplicados mientras hay uno en curso.
      if (pending.current) return false;
      pending.current = true;
      setState({ status: "pending" });
      try {
        const data = await action(input);
        setState({ status: "success", data });
        return true;
      } catch (error) {
        setState({ status: "error", error });
        return false;
      } finally {
        pending.current = false;
      }
    },
    [action],
  );

  const reset = useCallback(() => setState({ status: "idle" }), []);

  return { ...state, submit, reset };
}
