import type { ProblemDetails } from "./types";

/** Mensajes por código cuando la API no envía un título. */
const MENSAJES_POR_ESTADO: Record<number, string> = {
  400: "La solicitud contiene datos inválidos.",
  403: "La operación no está permitida.",
  404: "El recurso solicitado no existe.",
  409: "La operación entra en conflicto con datos existentes.",
  500: "Ocurrió un error inesperado en el servidor.",
};

const MENSAJE_SIN_CONEXION = "No se pudo conectar con el servidor. Comprueba tu conexión.";
const MENSAJE_GENERICO = "Ocurrió un error inesperado.";

/**
 * Error devuelto por el cliente de la API. `status` vale 0 cuando la
 * solicitud no llegó a recibir respuesta (red caída, CORS, servidor apagado).
 */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | undefined;

  constructor(status: number, problem?: ProblemDetails, options?: ErrorOptions) {
    super(problem?.detail ?? problem?.title ?? mensajePorEstado(status), options);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** `true` si el servidor no respondió. */
  get isNetworkError(): boolean {
    return this.status === 0;
  }

  /** Errores de validación por campo; vacío si la respuesta no los incluye. */
  get fieldErrors(): Record<string, string[]> {
    return this.problem?.errors ?? {};
  }
}

function mensajePorEstado(status: number): string {
  if (status === 0) return MENSAJE_SIN_CONEXION;
  return (
    MENSAJES_POR_ESTADO[status] ?? (status >= 500 ? MENSAJES_POR_ESTADO[500] : MENSAJE_GENERICO)
  );
}

/** Título y detalle legibles para mostrar cualquier error al usuario. */
export interface ErrorDescription {
  title: string;
  detail?: string;
  fieldErrors: Record<string, string[]>;
}

export function describeError(error: unknown): ErrorDescription {
  if (error instanceof ApiError) {
    const title = error.problem?.title ?? mensajePorEstado(error.status);
    const detail = error.problem?.detail;
    return {
      title,
      detail: detail && detail !== title ? detail : undefined,
      fieldErrors: error.fieldErrors,
    };
  }

  return { title: MENSAJE_GENERICO, fieldErrors: {} };
}
